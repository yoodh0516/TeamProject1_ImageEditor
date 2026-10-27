using Microsoft.ML.OnnxRuntime;

namespace ImageEditor.Models.AI
{
    /// <summary>
    /// Creates the local AI session with a conservative CPU default. DirectML is
    /// opt-in because older integrated GPUs can be slower than the CPU for the
    /// small 128px tiles used by these models. If DirectML cannot start, CPU is
    /// used automatically so image enhancement remains available.
    /// </summary>
    internal static class OnnxInferenceSessionFactory
    {
        private const string ProviderEnvironmentVariable =
            "IMAGEEDITOR_ONNX_PROVIDER";

        internal static InferenceSession Create(string modelPath)
        {
            string? requestedProvider = Environment.GetEnvironmentVariable(
                ProviderEnvironmentVariable);

            if (string.Equals(
                    requestedProvider,
                    "directml",
                    StringComparison.OrdinalIgnoreCase)) 
            {
                try
                {
                    using SessionOptions options = new SessionOptions
                    {
                        GraphOptimizationLevel =
                            GraphOptimizationLevel.ORT_ENABLE_ALL,
                        EnableMemoryPattern = false,
                        ExecutionMode = ExecutionMode.ORT_SEQUENTIAL
                    };
                    options.AppendExecutionProvider_DML(0);
                    return new InferenceSession(modelPath, options);
                }
                catch (Exception)
                {
                    // Some graphics drivers do not support this ONNX model. CPU
                    // execution is the safe fallback and preserves the result.
                }
            }

            return new InferenceSession(modelPath);
        }
    }
}
