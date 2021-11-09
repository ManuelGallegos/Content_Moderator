using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Microsoft.Azure.CognitiveServices.ContentModerator;
using Microsoft.Azure.CognitiveServices.ContentModerator.Models;



namespace ContentModerator
{
    class Program
    {

        // The name of the file that contains the text to be evaluted
        private static string TextFile = "TextFile.txt";

        // Name of the file that cointans the output of the evalution
        private static string OutputFile_Txt = "TextModerationOutput.txt";

        // The name of the file that contains the image URLs to evalute
        private static string ImageUrlFile = "ImageFile.txt";

        // Name of the file to contain the output from the evaluation
        private static string OutputFile_Img = "ModerationOutput.json";


        static void Main(string[] args)
        {


            TextDetection();
            ImageDetection();

        }


        private static void TextDetection()
        {

            // Load the input text
            string text = File.ReadAllText(TextFile);
            Console.WriteLine("Screening {0}", TextFile);

            text = text.Replace(System.Environment.NewLine, " ");
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(text);
            MemoryStream stream = new MemoryStream(byteArray);

            //Save the moderation results to a file
            using (StreamWriter outputWriter = new StreamWriter(OutputFile_Txt, false))
            {
                // Create a Content Moderator client and evalute the text
                using (var client = Client_Txt.NewClient())
                {
                    // Screen the input text: A) Check for profanity,
                    // B) autocorrect text, C) check for personally identifying
                    // information (PII) and D) classify the text into three categories

                    outputWriter.WriteLine("Autocorrect typos, check for matching terms, PII, and classify.");
                    var screenResult = client.TextModeration.ScreenText("text/plain", stream, "eng", true, true, null, true);
                    outputWriter.WriteLine(JsonConvert.SerializeObject(screenResult, Formatting.Indented));

                }

                outputWriter.Flush();
                outputWriter.Close();
            }

        }



        public static class Client_Txt
        {

            // the base URL fragment for content Moderator calls.
            // Add your Azure Content Moderator endpoint to your environment variables

            private static readonly string AzureBaseUrl = Environment.GetEnvironmentVariable("CONTENT_MODERATOR_ENDPOINT");

            // Your Content Moderator subscription key.
            //Add your Azure Content Moderator subscription key to your environment variables.

            private static readonly string CMSubscriptionKey = Environment.GetEnvironmentVariable("CONTENT_MODERATOR_SUBSCRIPTION_KEY");

            // Returns a new Content Moderator client for your subscription.
            public static ContentModeratorClient NewClient()
            {
                // Create and initialize an instance of the Content Moderator API wrapper.
                ContentModeratorClient client = new ContentModeratorClient(new ApiKeyServiceClientCredentials(CMSubscriptionKey));

                client.Endpoint = AzureBaseUrl;
                return client;  
            }

        }


        public static void ImageDetection()
        {

            // Create and object to store image moderation results
            List<EvaluationData> evaluationData = new List<EvaluationData>();

            // Create an instance of the Content Moderator API Wrapper
            using (var client = Client_Img.NewClient())
            {
                // Read image URLs from the input file and evalute each one
                using (StreamReader streamReader = new StreamReader(ImageUrlFile))
                {
                    while(!streamReader.EndOfStream)
                    {
                        string line = streamReader.ReadLine().Trim();
                        if(line != string.Empty)
                        {
                            EvaluationData imageData = EvaluateImage(client, line);
                            evaluationData.Add(imageData);


                        }
                    }
                }
            }

            // Save the moderation results to a file
            using(StreamWriter streamWriter = new StreamWriter(OutputFile_Img, false))
            {
                streamWriter.WriteLine(JsonConvert.SerializeObject(evaluationData, Formatting.Indented));

                streamWriter.Flush();
                streamWriter.Close();

            }


        }


        private static EvaluationData EvaluateImage(ContentModeratorClient client, string imageUrl)
        {
            var url = new BodyModel("URL", imageUrl.Trim());

            var imageData = new EvaluationData();

            imageData.ImageUrl = url.Value;

            // Evaluate for adult and racy content
            imageData.ImageModeration = client.ImageModeration.EvaluateUrlInput("application/json", url, true);
            Thread.Sleep(1000);

            // Detect and extract text
            imageData.TextDetection = client.ImageModeration.OCRUrlInput("eng", "application/json", url, true);
            Thread.Sleep(1000);

            // Detect faces
            imageData.FaceDetection = client.ImageModeration.FindFacesUrlInput("application/json", url, true);
            Thread.Sleep(1000);

            return imageData;


        }



        public static class Client_Img
        {
            // the base URL fragment for content Moderator calls.
            // Add your Azure Content Moderator endpoint to your environment variables

            private static readonly string AzureBaseUrl = Environment.GetEnvironmentVariable("FACE_ENDPOINT");

            // Your Content Moderator subscription key.
            //Add your Azure Content Moderator subscription key to your environment variables.

            private static readonly string CMSubscriptionKey = Environment.GetEnvironmentVariable("CONTENT_MODERATOR_SUBSCRIPTION_KEY");

            // Returns a new Content Moderator client for your subscription.
            public static ContentModeratorClient NewClient()
            {
                // Create and initialize an instance of the Content Moderator API wrapper.
                ContentModeratorClient client = new ContentModeratorClient(new ApiKeyServiceClientCredentials(CMSubscriptionKey));

                client.Endpoint = AzureBaseUrl;
                return client;
            }

        }


        // Contains the image moderation results for an image
        // including text and face detetion results.

        public class EvaluationData
        {
            // the URL of the evaluated image
            public string ImageUrl;

            // The image moderation results
            public Evaluate ImageModeration;

            // the text detection results.
            public OCR TextDetection;

            // the face detection results
            public FoundFaces FaceDetection;
        }


    }
}
