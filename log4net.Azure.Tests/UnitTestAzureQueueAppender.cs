﻿using log4net.Appender;
using log4net.Core;
using NUnit.Framework;
using System;
using System.Diagnostics;

namespace log4net.Azure.Tests
{
    [TestFixture]
    public class UnitTestAzureQueueAppender
    {
        private AzureQueueAppender _appender;

        [OneTimeSetUp]
        public void Initialize()
        {
            _appender = new AzureQueueAppender()
            {
                ConnectionString = "UseDevelopmentStorage=true",
                QueueName = "testloggingqueue"
            };
            _appender.ActivateOptions();
        }

        [Test]
        public void Test_Queue_Appender()
        {
            var @event = MakeEvent();

            _appender.DoAppend(@event);            
        }

        [Test]
        public void Test_Message_With_Exception()
        {
            const string message = "Exception to follow on other line";
            var ex = new Exception("This is the exception message");

            var @event = new LoggingEvent(null, null, "testLoggerName", Level.Critical, message, ex);

            _appender.DoAppend(@event);
        }

        [Test]
        public void Test_Queue_Appender_Multiple_5()
        {
            _appender.DoAppend(MakeEvents(5));
        }

        [Test]
        public void Test_Queue_Appender_Multiple_10()
        {
            _appender.DoAppend(MakeEvents(10));
        }

        [Test]
        public void Test_Queue_Appender_Multiple_100()
        {
            _appender.DoAppend(MakeEvents(100));
        }

        [Test]
        public void Test_Queue_Appender_Multiple_1000()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            _appender.DoAppend(MakeEvents(1000));
            sw.Stop();
        }

        private static LoggingEvent[] MakeEvents(int number)
        {
            var result = new LoggingEvent[number];
            for (int i = 0; i < number; i++)
            {
                result[i] = MakeEvent();
            }
            return result;
        }

        private static LoggingEvent MakeEvent()
        {
            return new LoggingEvent(
                new LoggingEventData
                {
                    Domain = "testDomain",
                    Identity = "testIdentity",
                    Level = Level.Critical,
                    LoggerName = "testLoggerName",
                    Message = "testMessage",
                    ThreadName = "testThreadName",
                    TimeStampUtc = DateTime.UtcNow,
                    UserName = "testUsername",
                    LocationInfo = new LocationInfo("className", "methodName", "fileName", "lineNumber")
                }
                );
        }
    }
}
