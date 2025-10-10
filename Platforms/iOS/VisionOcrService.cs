#if IOS
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using Vision;
using CoreImage;

namespace AgeVerification.Platforms.iOS
{
    public class VisionOcrService
    {
        public async Task<string> RecognizeTextAsync(string imagePath)
        {
            var tcs = new TaskCompletionSource<string>();

            try
            {
                using var image = UIImage.FromFile(imagePath);
                if (image == null)
                {
                    Console.WriteLine("❌ VisionOCR: Failed to load image");
                    tcs.SetResult(string.Empty);
                    return await tcs.Task;
                }

                using var ciImage = new CIImage(image);
                var request = new VNRecognizeTextRequest((req, err) =>
                {
                    if (err != null)
                    {
                        Console.WriteLine($"❌ VisionOCR Error: {err.LocalizedDescription}");
                        tcs.TrySetResult(string.Empty);
                        return;
                    }

                    var observations = req.GetResults<VNRecognizedTextObservation>();
                    List<string> lines = new();

                    foreach (var observation in observations)
                    {
                        var topCandidate = observation.TopCandidates(1);
                        if (topCandidate.Length > 0)
                            lines.Add(topCandidate[0].String);
                    }

                    string fullText = string.Join("\n", lines);
                    Console.WriteLine("✅ Vision OCR result:\n" + fullText);
                    tcs.TrySetResult(fullText);
                })
                {
                    RecognitionLevel = VNRequestTextRecognitionLevel.Accurate,
                    UsesLanguageCorrection = true
                };

                var handler = new VNImageRequestHandler(ciImage, new NSDictionary());

                // This must be run on a background thread
                await Task.Run(() =>
                {
                    handler.Perform(new VNRequest[] { request }, out NSError error);
                    if (error != null)
                        Console.WriteLine($"❌ VisionOCR Handler Error: {error.LocalizedDescription}");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ VisionOCR Exception: {ex}");
                tcs.TrySetResult(string.Empty);
            }

            return await tcs.Task;
        }
    }
}
#endif
