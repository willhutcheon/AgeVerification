#if IOS
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using Vision;
using CoreGraphics;

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
                    Console.WriteLine("VisionOCR: Failed to load image");
                    tcs.SetResult(string.Empty);
                    return await tcs.Task;
                }

                var ciImage = new CoreImage.CIImage(image);
                var request = new VNRecognizeTextRequest((request, error) =>
                {
                    if (error != null)
                    {
                        Console.WriteLine($"VisionOCR Error: {error.LocalizedDescription}");
                        tcs.TrySetResult(string.Empty);
                        return;
                    }

                    var observations = request.GetResults<VNRecognizedTextObservation>();
                    List<string> lines = new();

                    foreach (var observation in observations)
                    {
                        var topCandidate = observation.TopCandidates(1);
                        if (topCandidate.Length > 0)
                            lines.Add(topCandidate[0].String);
                    }

                    string fullText = string.Join("\n", lines);
                    tcs.TrySetResult(fullText);
                });

                request.RecognitionLevel = VNRequestTextRecognitionLevel.Accurate;
                request.UsesLanguageCorrection = true;

                var handler = new VNImageRequestHandler(ciImage, new NSDictionary());
                //await handler.PerformRequestsAsync(new VNRequest[] { request });
                //handler.PerformRequests(new VNRequest[] { request }, out NSError error);
                handler.Perform(requests: new VNRequest[] { request }, error: out NSError error);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"VisionOCR Exception: {ex}");
                tcs.TrySetResult(string.Empty);
            }

            return await tcs.Task;
        }
    }
}
#endif
