using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using Plugin.Maui.OCR;
using System;
using System.Text.RegularExpressions;
using Plugin.Maui.OCR;
using System.Linq;

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
        private async void CaptureLicenseButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.CapturePhotoAsync();
                if (photo != null)
                {
                    LicensePreview.Source = ImageSource.FromFile(photo.FullPath);
                    licenseImagePath = photo.FullPath;
                    extractedDob = await ExtractDOBWithPluginOcrAsync(licenseImagePath);
                    await DisplayAlert("Date of Birth", $"The date of birth extracted was {extractedDob}.", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing license: {ex}");
            }
        }
        //private async Task<DateTime?> ExtractDOBWithPluginOcrAsync(string imagePath)
        //{
        //    try
        //    {
        //        byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
        //        var result = await _ocrService.RecognizeTextAsync(imageBytes);
        //        // result.Lines is already a list of strings
        //        string fullText = string.Join("\n", result.Lines);
        //        Console.WriteLine($"OCR text: {fullText}");
        //        var regex = new Regex(@"\b\d{1,2}/\d{1,2}/\d{2,4}\b");
        //        var match = regex.Match(fullText);
        //        if (match.Success && DateTime.TryParse(match.Value, out var dob))
        //            return dob;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"OCR failed: {ex}");
        //    }
        //    return null;
        //}
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
        private async Task<string> PrepareTessDataAsync()
        {
            string tessDir = Path.Combine(FileSystem.AppDataDirectory, "tessdata");
            Console.WriteLine($"Preparing tessdata in: {tessDir}");
            try
            {
                if (!Directory.Exists(tessDir))
                    Directory.CreateDirectory(tessDir);
                string destFile = Path.Combine(tessDir, "eng.traineddata");
                Console.WriteLine($"Destination file path: {destFile}");
                if (!File.Exists(destFile))
                {
                    Console.WriteLine("Copying eng.traineddata from assets...");
                    using var assetStream = await FileSystem.OpenAppPackageFileAsync("tessdata/eng.traineddata");
                    using var fileStream = File.Create(destFile);
                    await assetStream.CopyToAsync(fileStream);
                    Console.WriteLine("Copy complete.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in PrepareTessDataAsync: {ex}");
            }
            return tessDir;
        }

        private async Task<DateTime?> ExtractDOBWithPluginOcrAsync(string imagePath)
        {
            try
            {
                byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
                var result = await _ocrService.RecognizeTextAsync(imageBytes);
                string fullText = string.Join("\n", result.Lines);
                Console.WriteLine($"OCR text: {fullText}");
                // Match MM/DD/YY or MM/DD/YYYY
                var regex = new Regex(@"\b\d{1,2}/\d{1,2}/\d{2,4}\b");
                var match = regex.Match(fullText);
                if (match.Success)
                {
                    string dateStr = match.Value;
                    Console.WriteLine($"Found DOB candidate: {dateStr}");
                    DateTime dob;
                    string[] formats = { "MM/dd/yy", "M/d/yy", "MM/dd/yyyy", "M/d/yyyy" };
                    if (DateTime.TryParseExact(dateStr, formats,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out dob))
                    {
                        // Fix century for 2-digit years
                        if (dob.Year < 100)
                        {
                            int currentYear = DateTime.Now.Year % 100;
                            int century = (dob.Year <= currentYear ? 2000 : 1900);
                            dob = dob.AddYears(century - dob.Year);
                        }
                        return dob;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OCR failed: {ex}");
            }
            return null;
        }

    }
}