﻿using CAKafka.Domain.Models;
using CAKafka.Library.Subscriber.BackgroundListeners;
using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeBank3.ServiceLayer;

namespace TradeBank3.BackgroundListener
{
    public class BaselineListener : KafkaBackgroundConsumer<string>
    {
        private readonly ILogger<BaselineListener> _logger;
        private IUserInput _userInput;
        private ITradeAlgorithm _tradeAlgo;
        public BaselineListener(ITradeAlgorithm tradeAlgo, IUserInput userInput, ILogger<BaselineListener> logger, IOptions<KafkaOptions> options) : base(logger, options.Value, new List<string> { "TradeBaseline", "TradeOffer"})
        {
            _logger = logger;
            _userInput = userInput;
            _tradeAlgo = tradeAlgo;
        }

        public override async Task ProcessingLogic(IConsumer<string, string> consumer, ConsumeResult<string, string> message)
        {
            try
            {
                dynamic kafkaMessage = JsonConvert.DeserializeObject(message.Value);

                _logger.LogInformation($"message {message.Value}");
                if (kafkaMessage.tradeId != null)
                {
                    _tradeAlgo.ShouldAcceptTrade((Models.UserInput)kafkaMessage);
                    await _userInput.AddUserInput((Models.UserInput)kafkaMessage);
                }
                else if (kafkaMessage.recordId != null)
                {
                    _tradeAlgo.ComputeBaselinePPU((Models.Baseline)kafkaMessage);
                }
                else
                {
                    _logger.LogInformation("ERROR, wrong format");
                    _logger.LogInformation($"message {message.Value}");
                }

                consumer.Commit(message);
            }
            catch (Exception e)
            {

            }
        }
    }
}
