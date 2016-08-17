﻿using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Tests.Mocks;
using Helsenorge.Registries.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Messaging.Tests.ServiceBus.Senders
{
	[TestClass]
	public class AsynchronousSendTests : BaseTest
	{
		private OutgoingMessage CreateMessage()
		{
			return  new OutgoingMessage()
			{
				ToHerId = MockFactory.OtherHerId,
				CpaId = Guid.Empty,
				Payload = GenericMessage,
				MessageFunction = "DIALOG_INNBYGGER_EKONTAKT",
				MessageId = Guid.NewGuid().ToString("D"),
				ScheduledSendTimeUtc = DateTime.Now,
				PersonalId = "12345"
			};
		}
		[TestMethod]
		public void Send_Asynchronous_OK()
		{
			var message = CreateMessage();
			RunAndHandleException(Client.SendAndContinueAsync(Logger, message));

			Assert.AreEqual(1, MockFactory.OtherParty.Asynchronous.Messages.Count);
		}

		[TestMethod]
		public void Send_Asynchronous_Receipt()
		{
			var message = CreateMessage();
			message.ReceiptForMessageFunction = message.MessageFunction;
			message.MessageFunction = "APPREC";
			RunAndHandleException(Client.SendAndContinueAsync(Logger, message));

			Assert.AreEqual(1, MockFactory.OtherParty.Asynchronous.Messages.Count);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Send_Asynchronous_NoMessage()
		{
			RunAndHandleException(Client.SendAndContinueAsync(Logger, null));
		}
		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void Send_Asynchronous_Error_Missing_ToHerId()
		{
			var message = CreateMessage();
			message.ToHerId = 0;
			RunAndHandleException(Client.SendAndContinueAsync(Logger, message));
		}
		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Send_Asynchronous_Error_Missing_MessageId()
		{
			var message = CreateMessage();
			message.MessageId = null;
			RunAndHandleException(Client.SendAndContinueAsync(Logger, message));
		}
		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Send_Asynchronous_Error_Missing_MessageFunction()
		{
			var message = CreateMessage();
			message.MessageFunction = null;
			RunAndHandleException(Client.SendAndContinueAsync(Logger, message));
		}
		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Send_Asynchronous_Error_Missing_Payload()
		{
			var message = CreateMessage();
			message.Payload = null;
			RunAndHandleException(Client.SendAndContinueAsync(Logger, message));
		}
		
		[TestMethod]
		[ExpectedException(typeof(MessagingException))]
		public void Send_Asynchronous_Error_InvalidMessageFunction()
		{
			var message = CreateMessage();
			message.MessageFunction = "BOB";
			RunAndHandleMessagingException(Client.SendAndContinueAsync(Logger, message), EventIds.InvalidMessageFunction);
		}
		[TestMethod]
		[ExpectedException(typeof(MessagingException))]
		public void Send_Asynchronous_InvalidEncryption()
		{
			Settings.IgnoreCertificateErrorOnSend = false;
			CertificateValidator.SetError((c,u)=> (u == X509KeyUsageFlags.DataEncipherment) ? CertificateErrors.StartDate : CertificateErrors.None);

			var message = CreateMessage();
			RunAndHandleMessagingException(Client.SendAndContinueAsync(Logger, message), EventIds.RemoteCertificate);
		}
		[TestMethod]
		public void Send_Asynchronous_InvalidEncryption_Ignore()
		{
			Settings.IgnoreCertificateErrorOnSend = true;
			CertificateValidator.SetError((c, u) => (u == X509KeyUsageFlags.DataEncipherment) ? CertificateErrors.StartDate : CertificateErrors.None);

			var message = CreateMessage();
			RunAndHandleException(Client.SendAndContinueAsync(Logger, message));
		}
		[TestMethod]
		[ExpectedException(typeof(MessagingException))]
		public void Send_Asynchronous_InvalidSignature()
		{
			Settings.IgnoreCertificateErrorOnSend = false;
			CertificateValidator.SetError((c, u) => (u == X509KeyUsageFlags.NonRepudiation) ? CertificateErrors.StartDate : CertificateErrors.None);

			var message = CreateMessage();
			RunAndHandleMessagingException(Client.SendAndContinueAsync(Logger, message), EventIds.LocalCertificate);
		}

		private static void RunAndHandleException(Task task)
		{
			try
			{
				Task.WaitAll(task);
			}
			catch (AggregateException ex)
			{

				throw ex.InnerException;
			}
		}
		
		private static void RunAndHandleMessagingException(Task task, EventId id)
		{
			try
			{
				Task.WaitAll(task);
			}
			catch (AggregateException ex)
			{
				var messagingException = ex.InnerException as MessagingException;
				if ((messagingException != null) && (messagingException.EventId.Id == id.Id))
				{
					throw ex.InnerException;
				}

				throw new InvalidOperationException("Expected a messaging exception");
			}
		}
		
	}
}