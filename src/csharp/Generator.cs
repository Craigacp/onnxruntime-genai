// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.ML.OnnxRuntimeGenAI
{
    public class Generator : IDisposable
    {
        private IntPtr _generatorHandle;
        private bool _disposed = false;

        public Generator(Model model, GeneratorParams generatorParams)
        {
            Result.VerifySuccess(NativeMethods.OgaCreateGenerator(model.Handle, generatorParams.Handle, out _generatorHandle));
        }

        public bool IsDone()
        {
            return NativeMethods.OgaGenerator_IsDone(_generatorHandle);
        }

        public void ComputeLogits()
        {
            Result.VerifySuccess(NativeMethods.OgaGenerator_ComputeLogits(_generatorHandle));
        }

        public void GenerateNextToken()
        {
            Result.VerifySuccess(NativeMethods.OgaGenerator_GenerateNextToken(_generatorHandle));
        }

        public void GenerateNextTokenTop()
        {
            Result.VerifySuccess(NativeMethods.OgaGenerator_GenerateNextToken_Top(_generatorHandle));
        }

        public void GenerateNextTokenTopK(int k, float temperature)
        {
            Result.VerifySuccess(NativeMethods.OgaGenerator_GenerateNextToken_TopK(_generatorHandle, k, temperature));
        }

        public void GenerateNextTokenTopP(float p, float temperature)
        {
            Result.VerifySuccess(NativeMethods.OgaGenerator_GenerateNextToken_TopP(_generatorHandle, p, temperature));
        }

        public void GenerateNextTokenTopKTopP(int k, float p, float temperature)
        {
            Result.VerifySuccess(NativeMethods.OgaGenerator_GenerateNextToken_TopK_TopP(_generatorHandle, k, p, temperature));
        }

        public ReadOnlySpan<int> GetSequence(ulong index)
        {
            ulong sequenceLength = NativeMethods.OgaGenerator_GetSequenceLength(_generatorHandle, (UIntPtr)index).ToUInt64();
            IntPtr sequencePtr = NativeMethods.OgaGenerator_GetSequence(_generatorHandle, (UIntPtr)index);
            unsafe
            {
                return new ReadOnlySpan<int>(sequencePtr.ToPointer(), (int)sequenceLength);
            }
        }

        ~Generator()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            NativeMethods.OgaDestroyGenerator(_generatorHandle);
            _generatorHandle = IntPtr.Zero;
            _disposed = true;
        }
    }
}
