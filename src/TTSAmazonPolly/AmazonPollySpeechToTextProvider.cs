﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon.Runtime;
using Common;
using log4net;
using Gender = Common.Gender;

namespace TTSAmazonPolly
{
    public class AmazonPollySpeechToTextProvider : ITextToSpeechProvider {

        private static readonly ILog Log = LogManager.GetLogger(typeof(AmazonPollySpeechToTextProvider));

        private readonly AmazonPollyClient _client;

        public AmazonPollySpeechToTextProvider(string accessKey, string secretKey)
        {
            _client = new AmazonPollyClient(new BasicAWSCredentials(accessKey, secretKey), RegionEndpoint.EUCentral1);
        }
        public string Name => "Amazon Polly";
        public string FileExtension => "wav";

        public async Task<Stream> SynthesizeTextToStreamAsync(IVoice voice, string text)
        {
            var pollyVoice = (AmazonPollyVoice) voice;
            var request = new SynthesizeSpeechRequest()
            {
                Text = text,
                VoiceId = VoiceId.FindValue(pollyVoice.VoiceId),
                OutputFormat = OutputFormat.Mp3
            };

            var response = await _client.SynthesizeSpeechAsync(request);

            return response.AudioStream;
        }

        public async Task<IList<IVoice>> GetVoicesAsync()
        {
            var voices = await _client.DescribeVoicesAsync(new DescribeVoicesRequest());
            return voices.Voices.Select(voice =>
            {

                var result = new AmazonPollyVoice()
                {
                    Language = voice.LanguageName,
                    Name = voice.Name,
                    Gender = (Gender)Enum.Parse(typeof(Gender), voice.Gender.ToString()),
                    VoiceId = voice.Id
                };

                return result;

            }).Cast<IVoice>().ToList();
        }

        public bool IsAvailable => Task.Run(CheckAvailable).Result;

        private async Task<bool> CheckAvailable()
        {
            try
            {
                await GetVoicesAsync();
                return true;
            }
            catch (Exception e)
            {
                Log.Error("Cannot connect to Amazon Polly", e);
                return false;
            }
        }
    }
}