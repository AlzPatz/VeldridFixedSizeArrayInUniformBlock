using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.Utilities;
using Veldrid.StartupUtilities;
using Veldrid.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace arrayuniform
{
    public struct Vertex
    {
        public Vector2 Position;
        public Vector2 TexCoord;

        public Vertex(Vector2 pos, Vector2 tex)
        {
            Position = pos;
            TexCoord = tex;
        }
        public const int SizeInBytes = 16;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ExampleStruct
    {
        [FieldOffset(0)]
        public Vector2 V0;
        [FieldOffset(8)]
        public Vector2 V1;

        public const int SizeInBytes = 16;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct UniformBlockStruct
    {
        [FieldOffset(0)]
        public ExampleStruct[] ArrayOfExampleStructs;
    }

    public class Demo
    {
        private const int UNIFORM_ARRAY_SIZE = 4; 
        private UniformBlockStruct _data;

        public void Run()
        {
            Init();

            Loop();
        }

        private Sdl2Window _window;
        private GraphicsDevice _device;
        private DisposeCollectorResourceFactory _factory;

        private DeviceBuffer _vbPosition;
        private ShaderSetDescription _shaderSetDescription;

        private DeviceBuffer _uniformBuffer;
        private ResourceSet _uniformResourceSet;
        
        private Pipeline _pipeline;

        private CommandList _cl;       

        private void Init()
        {
            WindowCreateInfo windowCI = new WindowCreateInfo()
            {
                X = 100,
                Y = 100,
                WindowWidth = 960,
                WindowHeight = 540,
                WindowTitle = "Testing Rendering using same textures switching resource slots between draws within same command list"
            };

            _window = VeldridStartup.CreateWindow(ref windowCI);

            _device = VeldridStartup.CreateGraphicsDevice(_window); 

            _factory = new DisposeCollectorResourceFactory(_device.ResourceFactory);

            Vertex[] vertices =
            {
                new Vertex(new Vector2(-1.0f, 1.0f), new Vector2(0.0f, 1.0f)),
                new Vertex(new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f)),
                new Vertex(new Vector2(-1.0f, -1.0f), new Vector2(0.0f, 0.0f)),

                new Vertex(new Vector2(-1.0f, -1.0f), new Vector2(0.0f, 0.0f)),
                new Vertex(new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f)),
                new Vertex(new Vector2(1.0f, -1.0f), new Vector2(1.0f, 0.0f)),
            };

            _vbPosition = _factory.CreateBuffer(new BufferDescription(12 * Vertex.SizeInBytes, BufferUsage.VertexBuffer));
            _device.UpdateBuffer(_vbPosition, 0, vertices);

            var layout = new VertexLayoutDescription
            (
                16,
                0,
                new VertexElementDescription[] {
                    new VertexElementDescription("Position", VertexElementFormat.Float2, VertexElementSemantic.Position),
                    new VertexElementDescription("VTex", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate)
                }
            );

            var vertexShader = LoadShader("Vertex", ShaderStages.Vertex, _device);
            var fragmentShader = LoadShader("Fragment", ShaderStages.Fragment, _device);

            _shaderSetDescription = new ShaderSetDescription(
                new[]
                {
                    layout
                },
                new[]
                {
                    vertexShader, fragmentShader
                }
            );

            var resourceLayoutDescription = new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("AUniformBlock", ResourceKind.UniformBuffer, ShaderStages.Fragment)
            );

            var resourceLayout = new ResourceLayout[]
            {
                _factory.CreateResourceLayout(resourceLayoutDescription)
            };

            _uniformBuffer = _factory.CreateBuffer(new BufferDescription(UNIFORM_ARRAY_SIZE * ExampleStruct.SizeInBytes, BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            _uniformResourceSet = _factory.CreateResourceSet(
                new ResourceSetDescription(resourceLayout[0], _uniformBuffer)
            );

            _data = new UniformBlockStruct
            {
                ArrayOfExampleStructs = new ExampleStruct[]
                {
                    new ExampleStruct() 
                    {
                        V0 = Vector2.One,
                        V1 = Vector2.One
                    },
                    new ExampleStruct() 
                    {
                        V0 = Vector2.One,
                        V1 = Vector2.One
                    },
                    new ExampleStruct() 
                    {
                        V0 = new Vector2(300.0f, 400.0f),
                        V1 = new Vector2(100.0f, 200.0f)
                    },
                    new ExampleStruct() 
                    {
                        V0 = Vector2.One,
                        V1 = Vector2.One
                    }
                } 
            };

            var pipelineDescription = new GraphicsPipelineDescription()
            {
                BlendState = BlendStateDescription.SingleAlphaBlend,
                DepthStencilState = new DepthStencilStateDescription(
                    depthTestEnabled: true, 
                    depthWriteEnabled: true,
                    comparisonKind: ComparisonKind.LessEqual),
                RasterizerState = new RasterizerStateDescription(
                    cullMode: FaceCullMode.None, 
                    fillMode: PolygonFillMode.Solid,
                    frontFace: FrontFace.Clockwise,
                    depthClipEnabled: true, 
                    scissorTestEnabled: false
                ),
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                ResourceLayouts = resourceLayout,
                ShaderSet = _shaderSetDescription,
                Outputs = _device.SwapchainFramebuffer.OutputDescription
            };

            _pipeline = _factory.CreateGraphicsPipeline(pipelineDescription);

            _cl = _factory.CreateCommandList();
        }

        private void Loop()
        {
            while(_window.Exists)
            {
                _window.PumpEvents();
                Render();
            }
        }

        private void Render()
        {
            _device.UpdateBuffer(_uniformBuffer, 0, ref _data);

            _cl.Begin();

            _cl.SetFramebuffer(_device.SwapchainFramebuffer);

            _cl.ClearColorTarget(0, RgbaFloat.CornflowerBlue);

            _cl.SetVertexBuffer(0, _vbPosition);

            _cl.SetPipeline(_pipeline);

            _cl.SetGraphicsResourceSet(0, _uniformResourceSet);
            
            _cl.Draw(6, 1, 0, 0);

            _cl.End();

            _device.SubmitCommands(_cl);

            _device.SwapBuffers();
        }

        public static Shader LoadShader(string name, ShaderStages stage, GraphicsDevice device)
        {
            string extension = null;
            switch (device.BackendType)
            {
                case GraphicsBackend.Direct3D11:
                    extension = "hlsl.bytes";
                    break;
                case GraphicsBackend.Vulkan:
                    extension = "spv";
                    break;
                case GraphicsBackend.OpenGL:
                    extension = "glsl";
                    break;
                default: throw new System.InvalidOperationException();
            }

            string entryPoint = stage == ShaderStages.Vertex ? "VS" : "FS";
            string path = Path.Combine(System.AppContext.BaseDirectory, "Shaders", $"{name}.{extension}");
            byte[] shaderBytes = File.ReadAllBytes(path);

            return device.ResourceFactory.CreateShader(new ShaderDescription(stage, shaderBytes, entryPoint));
        }
    }
}