using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using Plugin.Maui.OCR;
using System;
using System.Text.RegularExpressions;
using Plugin.Maui.OCR;
using System.Linq;
using SkiaSharp;

namespace AgeVerification
{
    public partial class MainPage : ContentPage
    {
        private string licenseImagePath;
        private string selfieImagePath;
        private DateTime? extractedDob;
        private readonly IOcrService _ocrService;
        public MainPage()
        {
            InitializeComponent();
            _ocrService = OcrPlugin.Default;
        }
        //private async void CaptureLicenseButton_Clicked(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        //var photo = await MediaPicker.CapturePhotoAsync();
        //        //if (photo != null)
        //        //{
        //        //    LicensePreview.Source = ImageSource.FromFile(photo.FullPath);
        //        //    licenseImagePath = photo.FullPath;
        //        //    extractedDob = await ExtractDOBWithPluginOcrAsync(licenseImagePath);
        //        //    await DisplayAlert("Date of Birth", $"The date of birth extracted was {extractedDob}.", "OK");
        //        //}
        //        var photo = await MediaPicker.CapturePhotoAsync();
        //        if (photo != null)
        //        {
        //            using var stream = await photo.OpenReadAsync();
        //            LicensePreview.Source = ImageSource.FromStream(() => stream);
        //            licenseImagePath = photo.FullPath; // still needed for OCR
        //            extractedDob = await ExtractDOBWithPluginOcrAsync(licenseImagePath);
        //            await DisplayAlert("Date of Birth", $"The date of birth extracted was {extractedDob}.", "OK");
        //        }
        //        DateTime? dob = await ExtractDOBWithPluginOcrAsync(licenseImagePath);
        //        if (dob != null)
        //        {
        //            int age = CalculateAge(dob.Value);

        //            if (age >= 21)
        //                await DisplayAlert("Age Verification", $"User is {age} years old (21+)", "OK");
        //            else
        //                await DisplayAlert("Age Verification", $"User is only {age} years old (<21)", "OK");
        //        }
        //        else
        //        {
        //            await DisplayAlert("Error", "Could not extract DOB from the license.", "OK");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error capturing license: {ex}");
        //    }
        //}

        //private byte[] FixOrientationAndResize(Stream imageStream)
        //{
        //    using var managedStream = new SKManagedStream(imageStream);
        //    using var codec = SKCodec.Create(managedStream);
        //    var info = codec.Info;
        //    using var bitmap = SKBitmap.Decode(codec);

        //    // Resize if too big (optional)
        //    int maxDim = 1024;
        //    float scale = Math.Min((float)maxDim / info.Width, (float)maxDim / info.Height);
        //    SKBitmap resizedBitmap = bitmap;
        //    if (scale < 1f)
        //        resizedBitmap = bitmap.Resize(new SKImageInfo((int)(info.Width * scale), (int)(info.Height * scale)), SKFilterQuality.High);

        //    using var image = SKImage.FromBitmap(resizedBitmap);
        //    using var ms = new MemoryStream();
        //    image.Encode(SKEncodedImageFormat.Jpeg, 90).SaveTo(ms);
        //    return ms.ToArray();
        //}
        private byte[] FixOrientationAndResize(Stream imageStream)
        {
            using var managedStream = new SKManagedStream(imageStream);
            using var codec = SKCodec.Create(managedStream);
            var info = codec.Info;

            using var bitmap = SKBitmap.Decode(codec);

            // Resize if too big (optional)
            int maxDim = 1024;
            float scale = Math.Min((float)maxDim / info.Width, (float)maxDim / info.Height);
            SKBitmap resizedBitmap = bitmap;
            if (scale < 1f)
                resizedBitmap = bitmap.Resize(new SKImageInfo((int)(info.Width * scale), (int)(info.Height * scale)), SKFilterQuality.High);

            // -------------------
            // Convert to grayscale
            for (int y = 0; y < resizedBitmap.Height; y++)
            {
                for (int x = 0; x < resizedBitmap.Width; x++)
                {
                    var color = resizedBitmap.GetPixel(x, y);
                    byte gray = (byte)((color.Red + color.Green + color.Blue) / 3);
                    resizedBitmap.SetPixel(x, y, new SKColor(gray, gray, gray));
                }
            }
            // -------------------

            using var image = SKImage.FromBitmap(resizedBitmap);
            using var ms = new MemoryStream();
            image.Encode(SKEncodedImageFormat.Jpeg, 90).SaveTo(ms);
            return ms.ToArray();
        }

        private async void CaptureLicenseButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.CapturePhotoAsync();
                if (photo != null)
                {
                    // Display the image in the Image control
                    //using var stream = await photo.OpenReadAsync();
                    //LicensePreview.Source = ImageSource.FromStream(() => stream);

                    //// Read the image bytes for OCR
                    //byte[] imageBytes;
                    //using (var ms = new MemoryStream())
                    //{
                    //    stream.Position = 0;
                    //    await stream.CopyToAsync(ms);
                    //    imageBytes = ms.ToArray();
                    //}

                    //// Extract DOB from image bytes (works on iOS and Android)
                    //extractedDob = await ExtractDOBWithPluginOcrAsync(imageBytes);


                    using var stream = await photo.OpenReadAsync();
                    byte[] imageBytes = FixOrientationAndResize(stream);
                    extractedDob = await ExtractDOBWithPluginOcrAsync(imageBytes);


                    if (extractedDob != null)
                    {
                        int age = CalculateAge(extractedDob.Value);
                        await DisplayAlert("Date of Birth", $"User is {age} years old", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Error", "Could not extract DOB from the license.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing license: {ex}");
            }
        }

        private async void CaptureSelfieButton_Clicked(object sender, EventArgs e)
        {
            var photo = await MediaPicker.CapturePhotoAsync();
            if (photo != null)
            {
                SelfiePreview.Source = ImageSource.FromFile(photo.FullPath);
                selfieImagePath = photo.FullPath;

                // TODO: Run Face Match
            }
        }
        private async void VerifyButton_Clicked(object sender, EventArgs e)
        {
            if (extractedDob == null)
            {
                await DisplayAlert("Error", "No DOB extracted. Please scan the license first.", "OK");
                return;
            }
            int age = CalculateAge(extractedDob.Value);
            if (age >= 21)
                await DisplayAlert("Verified", $"User is {age} years old ✅", "OK");
            else
                await DisplayAlert("Denied", $"User is only {age} years old ❌", "OK");
        }
        private void VerifyAge(DateTime dob)
        {
            int age = CalculateAge(dob);
            if (age >= 21)
                DisplayAlert("Verified", $"User is {age} years old.", "OK");
            else
                DisplayAlert("Denied", $"User is only {age} years old.", "OK");
        }
        private int CalculateAge(DateTime dob)
        {
            var today = DateTime.Today;
            var age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;
            return age;
        }
        //private async Task<DateTime?> ExtractDOBWithPluginOcrAsync(string imagePath)
        //{
        //    try
        //    {
        //        byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
        //        var result = await _ocrService.RecognizeTextAsync(imageBytes);
        //        string fullText = string.Join("\n", result.Lines);
        //        Console.WriteLine($"OCR text: {fullText}");
        //        // Match all dates (MM/DD/YY or MM/DD/YYYY)
        //        var regex = new Regex(@"\b\d{1,2}/\d{1,2}/\d{2,4}\b");
        //        var matches = regex.Matches(fullText);
        //        List<DateTime> dates = new List<DateTime>();
        //        string[] formats = { "MM/dd/yy", "M/d/yy", "MM/dd/yyyy", "M/d/yyyy" };
        //        foreach (Match match in matches)
        //        {
        //            string dateStr = match.Value.Replace(" ", ""); // clean any spaces
        //            if (DateTime.TryParseExact(dateStr, formats,
        //                System.Globalization.CultureInfo.InvariantCulture,
        //                System.Globalization.DateTimeStyles.None,
        //                out DateTime dob))
        //            {
        //                // Fix 2-digit years
        //                if (dob.Year < 100)
        //                {
        //                    int currentYear = DateTime.Now.Year % 100;
        //                    int century = (dob.Year <= currentYear ? 2000 : 1900);
        //                    dob = dob.AddYears(century - dob.Year);
        //                }
        //                dates.Add(dob);
        //            }
        //        }
        //        if (dates.Count > 0)
        //        {
        //            var oldestDate = dates.Min(); // the oldest date
        //            Console.WriteLine($"Oldest date found: {oldestDate}");
        //            return oldestDate;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"OCR failed: {ex}");
        //    }
        //    return null;
        //}
        private async Task<DateTime?> ExtractDOBWithPluginOcrAsync(byte[] imageBytes)
        {
            try
            {
                var result = await _ocrService.RecognizeTextAsync(imageBytes);
                string fullText = string.Join("\n", result.Lines);
                Console.WriteLine($"OCR text: {fullText}");

                var regex = new Regex(@"\b\d{1,2}/\d{1,2}/\d{2,4}\b");
                var matches = regex.Matches(fullText);
                List<DateTime> dates = new List<DateTime>();
                string[] formats = { "MM/dd/yy", "M/d/yy", "MM/dd/yyyy", "M/d/yyyy" };

                foreach (Match match in matches)
                {
                    string dateStr = match.Value.Replace(" ", "");
                    if (DateTime.TryParseExact(dateStr, formats,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out DateTime dob))
                    {
                        if (dob.Year < 100)
                        {
                            int currentYear = DateTime.Now.Year % 100;
                            int century = (dob.Year <= currentYear ? 2000 : 1900);
                            dob = dob.AddYears(century - dob.Year);
                        }
                        dates.Add(dob);
                    }
                }

                if (dates.Count > 0)
                    return dates.Min();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OCR failed: {ex}");
            }
            return null;
        }
    }
}