// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Microsoft.ML.OnnxRuntimeGenAI;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Microsoft.ML.OnnxRuntimeGenAI.Tests
{
    public partial class OnnxRuntimeGenAITests
    {
        private readonly ITestOutputHelper output;

        public OnnxRuntimeGenAITests(ITestOutputHelper o)
        {
            this.output = o;
        }

        private class IgnoreOnModelAbsebceFact : FactAttribute
        {
            public IgnoreOnModelAbsebceFact()
            {
                string modelPath = Path.Combine(Directory.GetCurrentDirectory(), "test_models", "cpu", "phi-2");
                bool exists = System.IO.Directory.Exists(modelPath);
                if (!System.IO.Directory.Exists(modelPath))
                {
                    // Skip this test on some machines since the model cannot be downloaded on those machines at runtime.
                    Skip = "Skipping this test since the model does not exist.";
                }
            }
        }

        [Fact(DisplayName = "TestGreedySearch")]
        public void TestGreedySearch()
        {
            ulong maxLength = 10;
            int[] inputIDs = new int[] { 0, 0, 0, 52, 0, 0, 195, 731 };
            var inputIDsShape = new ulong[] { 2, 4 };
            ulong batchSize = inputIDsShape[0];
            ulong sequenceLength = inputIDsShape[1];
            var expectedOutput = new int[] { 0, 0, 0, 52, 204, 204, 204, 204, 204, 204,
                                             0, 0, 195, 731, 731, 114, 114, 114, 114, 114 };

            string modelPath = Path.Combine(Directory.GetCurrentDirectory(), "test_models", "hf-internal-testing", "tiny-random-gpt2-fp32");
            using (var model = new Model(modelPath))
            {
                Assert.NotNull(model);
                using (var generatorParams = new GeneratorParams(model))
                {
                    Assert.NotNull(generatorParams);

                    generatorParams.SetSearchOption("max_length", maxLength);
                    generatorParams.SetInputIDs(inputIDs, sequenceLength, batchSize);

                    using (var generator = new Generator(model, generatorParams))
                    {
                        Assert.NotNull(generator);

                        while (!generator.IsDone())
                        {
                            generator.ComputeLogits();
                            generator.GenerateNextTokenTop();
                        }

                        for (ulong i = 0; i < batchSize; i++)
                        {
                            var sequence = generator.GetSequence(i).ToArray();
                            var expectedSequence = expectedOutput.Skip((int)i * (int)maxLength).Take((int)maxLength);
                            Assert.Equal(expectedSequence, sequence);
                        }
                    }

                    var sequences = model.Generate(generatorParams);
                    Assert.NotNull(sequences);

                    for (ulong i = 0; i < batchSize; i++)
                    {
                        var expectedSequence = expectedOutput.Skip((int)i * (int)maxLength).Take((int)maxLength);
                        Assert.Equal(expectedSequence, sequences[i].ToArray());
                    }
                }
            }
        }

        [IgnoreOnModelAbsebceFact(DisplayName = "TestTopKSearch")]
        public void TestTopKSearch()
        {
            int topK = 100;
            float temp = 0.6f;
            ulong maxLength = 40;
            
            string modelPath = Path.Combine(Directory.GetCurrentDirectory(), "test_models", "cpu", "phi-2");
            using (var model = new Model(modelPath))
            {
                Assert.NotNull(model);
                using (var tokenizer = new Tokenizer(model))
                {
                    Assert.NotNull(tokenizer);

                    var strings = new string[] {
                        "This is a test.",
                        "Rats are awesome pets!",
                        "The quick brown fox jumps over the lazy dog."
                    };

                    var sequences = tokenizer.EncodeBatch(strings);
                    Assert.NotNull(sequences);
                    Assert.Equal((ulong)strings.Length, sequences.NumSequences);

                    using GeneratorParams generatorParams = new GeneratorParams(model);
                    Assert.NotNull(generatorParams);

                    generatorParams.SetSearchOption("max_length", 20);
                    generatorParams.SetInputSequences(sequences);

                    using Generator generator = new Generator(model, generatorParams);
                    Assert.NotNull(generator);
                    while (!generator.IsDone())
                    {
                        generator.ComputeLogits();
                        generator.GenerateNextTokenTopK(topK, temp);
                    }

                    generatorParams.SetSearchOption("do_sample", true);
                    generatorParams.SetSearchOption("top_k", topK);
                    generatorParams.SetSearchOption("temperature", temp);
                    var outputSequences = model.Generate(generatorParams);
                    Assert.NotNull(outputSequences);

                    var outputStrings = tokenizer.DecodeBatch(outputSequences);
                    Assert.NotNull(outputStrings);
                }
            }
        }

        [IgnoreOnModelAbsebceFact(DisplayName = "TestTopPSearch")]
        public void TestTopPSearch()
        {
            float topP = 0.6f;
            float temp = 0.6f;
            ulong maxLength = 40;
            
            string modelPath = Path.Combine(Directory.GetCurrentDirectory(), "test_models", "cpu", "phi-2");
            using (var model = new Model(modelPath))
            {
                Assert.NotNull(model);
                using (var tokenizer = new Tokenizer(model))
                {
                    Assert.NotNull(tokenizer);

                    var strings = new string[] {
                        "This is a test.",
                        "Rats are awesome pets!",
                        "The quick brown fox jumps over the lazy dog."
                    };

                    var sequences = tokenizer.EncodeBatch(strings);
                    Assert.NotNull(sequences);
                    Assert.Equal((ulong)strings.Length, sequences.NumSequences);

                    using GeneratorParams generatorParams = new GeneratorParams(model);
                    Assert.NotNull(generatorParams);

                    generatorParams.SetSearchOption("max_length", 20);
                    generatorParams.SetInputSequences(sequences);

                    using Generator generator = new Generator(model, generatorParams);
                    Assert.NotNull(generator);
                    while (!generator.IsDone())
                    {
                        generator.ComputeLogits();
                        generator.GenerateNextTokenTopP(topP, temp);
                    }

                    generatorParams.SetSearchOption("do_sample", true);
                    generatorParams.SetSearchOption("top_p", topP);
                    generatorParams.SetSearchOption("temperature", temp);
                    var outputSequences = model.Generate(generatorParams);
                    Assert.NotNull(outputSequences);

                    var outputStrings = tokenizer.DecodeBatch(outputSequences);
                    Assert.NotNull(outputStrings);
                }
            }
        }

        [IgnoreOnModelAbsebceFact(DisplayName = "TestTopKTopPSearch")]
        public void TestTopKTopPSearch()
        {
            int topK = 100;
            float topP = 0.6f;
            float temp = 0.6f;
            ulong maxLength = 40;
            
            string modelPath = Path.Combine(Directory.GetCurrentDirectory(), "test_models", "cpu", "phi-2");
            using (var model = new Model(modelPath))
            {
                Assert.NotNull(model);
                using (var tokenizer = new Tokenizer(model))
                {
                    Assert.NotNull(tokenizer);

                    var strings = new string[] {
                        "This is a test.",
                        "Rats are awesome pets!",
                        "The quick brown fox jumps over the lazy dog."
                    };

                    var sequences = tokenizer.EncodeBatch(strings);
                    Assert.NotNull(sequences);
                    Assert.Equal((ulong)strings.Length, sequences.NumSequences);

                    using GeneratorParams generatorParams = new GeneratorParams(model);
                    Assert.NotNull(generatorParams);

                    generatorParams.SetSearchOption("max_length", 20);
                    generatorParams.SetInputSequences(sequences);

                    using Generator generator = new Generator(model, generatorParams);
                    Assert.NotNull(generator);
                    while (!generator.IsDone())
                    {
                        generator.ComputeLogits();
                        generator.GenerateNextTokenTopKTopP(topK, topP, temp);
                    }

                    generatorParams.SetSearchOption("do_sample", true);
                    generatorParams.SetSearchOption("top_k", topK);
                    generatorParams.SetSearchOption("top_p", topP);
                    generatorParams.SetSearchOption("temperature", temp);
                    var outputSequences = model.Generate(generatorParams);
                    Assert.NotNull(outputSequences);

                    var outputStrings = tokenizer.DecodeBatch(outputSequences);
                    Assert.NotNull(outputStrings);
                }
            }
        }

        [IgnoreOnModelAbsebceFact(DisplayName = "TestTokenizerBatchEncodeDecode")]
        public void TestTokenizerBatchEncodeDecode()
        {
            string modelPath = Path.Combine(Directory.GetCurrentDirectory(), "test_models", "cpu", "phi-2");
            using (var model = new Model(modelPath))
            {
                Assert.NotNull(model);
                using (var tokenizer = new Tokenizer(model))
                {
                    Assert.NotNull(tokenizer);

                    var strings = new string[] {
                        "This is a test.",
                        "Rats are awesome pets!",
                        "The quick brown fox jumps over the lazy dog."
                    };

                    var sequences = tokenizer.EncodeBatch(strings);

                    Assert.NotNull(sequences);
                    Assert.Equal((ulong)strings.Length, sequences.NumSequences);

                    string[] decodedStrings = tokenizer.DecodeBatch(sequences);
                    Assert.NotNull(decodedStrings);
                    Assert.Equal(strings, decodedStrings);
                }
            }
        }

        [IgnoreOnModelAbsebceFact(DisplayName = "TestTokenizerBatchEncodeSingleDecode")]
        public void TestTokenizerBatchEncodeSingleDecode()
        {
            string modelPath = Path.Combine(Directory.GetCurrentDirectory(), "test_models", "cpu", "phi-2");
            using (var model = new Model(modelPath))
            {
                Assert.NotNull(model);
                using (var tokenizer = new Tokenizer(model))
                {
                    Assert.NotNull(tokenizer);

                    var strings = new string[] {
                        "This is a test.",
                        "Rats are awesome pets!",
                        "The quick brown fox jumps over the lazy dog."
                    };

                    var sequences = tokenizer.EncodeBatch(strings);

                    Assert.NotNull(sequences);
                    Assert.Equal((ulong)strings.Length, sequences.NumSequences);

                    for (ulong i = 0; i < sequences.NumSequences; i++)
                    {
                        var decodedString = tokenizer.Decode(sequences[i]);
                        Assert.Equal(strings[i], decodedString);
                    }
                }
            }
        }

        [IgnoreOnModelAbsebceFact(DisplayName = "TestTokenizerBatchEncodeStreamDecode")]
        public void TestTokenizerBatchEncodeStreamDecode()
        {
            string modelPath = Path.Combine(Directory.GetCurrentDirectory(), "test_models", "cpu", "phi-2");
            using (var model = new Model(modelPath))
            {
                Assert.NotNull(model);
                using (var tokenizer = new Tokenizer(model))
                {
                    Assert.NotNull(tokenizer);
                    var tokenizerStream = tokenizer.CreateStream();

                    var strings = new string[] {
                        "This is a test.",
                        "Rats are awesome pets!",
                        "The quick brown fox jumps over the lazy dog."
                    };

                    var sequences = tokenizer.EncodeBatch(strings);

                    Assert.NotNull(sequences);
                    Assert.Equal((ulong)strings.Length, sequences.NumSequences);

                    for (ulong i = 0; i < sequences.NumSequences; i++)
                    {
                        string decodedString = "";
                        for (int j = 0; j < sequences[i].Length; j++)
                        {
                            decodedString += tokenizerStream.Decode(sequences[i][j]);
                        }
                        Assert.Equal(strings[i], decodedString);
                    }
                }
            }
        }

        [IgnoreOnModelAbsebceFact(DisplayName = "TestTokenizerSingleEncodeDecode")]
        public void TestTokenizerSingleEncodeDecode()
        {
            string modelPath = Path.Combine(Directory.GetCurrentDirectory(), "test_models", "cpu", "phi-2");
            using (var model = new Model(modelPath))
            {
                Assert.NotNull(model);
                using (var tokenizer = new Tokenizer(model))
                {
                    Assert.NotNull(tokenizer);
                    var tokenizerStream = tokenizer.CreateStream();

                    var str = "She sells sea shells by the sea shore.";

                    var sequences = tokenizer.Encode(str);

                    Assert.NotNull(sequences);

                    string decodedString = tokenizer.Decode(sequences[0]);
                    Assert.Equal(str, decodedString);
                }
            }
        }

        [IgnoreOnModelAbsebceFact(DisplayName = "TestPhi2")]
        public void TestPhi2()
        {
            string modelPath = Path.Combine(Directory.GetCurrentDirectory(), "test_models", "cpu", "phi-2");
            using (var model = new Model(modelPath))
            {
                Assert.NotNull(model);
                using (var tokenizer = new Tokenizer(model))
                {
                    Assert.NotNull(tokenizer);

                    var strings = new string[] {
                        "This is a test.",
                        "Rats are awesome pets!",
                        "The quick brown fox jumps over the lazy dog."
                    };

                    var sequences = tokenizer.EncodeBatch(strings);
                    Assert.NotNull(sequences);
                    Assert.Equal((ulong)strings.Length, sequences.NumSequences);

                    using GeneratorParams generatorParams = new GeneratorParams(model);
                    Assert.NotNull(generatorParams);

                    generatorParams.SetSearchOption("max_length", 20);
                    generatorParams.SetInputSequences(sequences);

                    var outputSequences = model.Generate(generatorParams);
                    Assert.NotNull(outputSequences);

                    var outputStrings = tokenizer.DecodeBatch(outputSequences);
                    Assert.NotNull(outputStrings);
                }
            }
        }
    }
}
