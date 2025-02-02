using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZendeskApi_v2;
using ZendeskApi_v2.Extensions;
using ZendeskApi_v2.Models.Brands;
using ZendeskApi_v2.Models.Constants;
using ZendeskApi_v2.Models.Shared;
using ZendeskApi_v2.Models.Tickets;
using ZendeskApi_v2.Requests;

namespace Tests
{
    [TestFixture]
    [Category("Tickets")]
    public class TicketTestsPart2
    {
        private readonly ZendeskApi api = new ZendeskApi(Settings.Site, Settings.AdminEmail, Settings.AdminPassword);
        private const string ExternalId = "this is a test";

        [OneTimeTearDown]
        public async Task TestCleanUp()
        {
            var response = await api.Tickets.GetTicketsByExternalIdAsync(ExternalId);
            foreach (var item in response.Tickets)
            {
                await api.Tickets.DeleteAsync(item.Id.Value);
            }
        }

        [Test]
        public async Task CanGetTicketsByExternalIdAsync()
        {
            var ticket = new Ticket
            {
                Subject = "my printer is on fire",
                Comment = new Comment { Body = "HELP", Public = true },
                Priority = TicketPriorities.Urgent,
                ExternalId = ExternalId
            };

            var resp1 = await api.Tickets.CreateTicketAsync(ticket);
            var response = await api.Tickets.GetTicketsByExternalIdAsync(ticket.ExternalId);

            Assert.That(response.Tickets.Count, Is.GreaterThan(0));
            Assert.That((await api.Tickets.DeleteAsync(resp1.Ticket.Id.Value)), Is.True);
        }

        [Test]
        public void CanGetTicketsByExternalId()
        {
            var ticket = new Ticket
            {
                Subject = "my printer is on fire",
                Comment = new Comment { Body = "HELP", Public = true },
                Priority = TicketPriorities.Urgent,
                ExternalId = ExternalId
            };

            var resp1 = api.Tickets.CreateTicket(ticket);
            var response = api.Tickets.GetTicketsByExternalId(ticket.ExternalId);

            Assert.That(response.Tickets.Count, Is.GreaterThan(0));
            Assert.That(api.Tickets.Delete(resp1.Ticket.Id.Value), Is.True);
        }

        [Test]
        public async Task CanBatchUpdateTickets()
        {
            var pendingStatus = "pending";

            var tickets = new List<Ticket>{ new Ticket
            {
                Subject = "Batch Update Ticket 1",
                Comment = new Comment { Body = "HELP", Public = true },
                Priority = TicketPriorities.Urgent,
                ExternalId = ExternalId
            },  new Ticket
            {
                Subject = "Batch Update Ticket 2",
                Comment = new Comment { Body = "HELP", Public = true },
                Priority = TicketPriorities.Urgent,
                ExternalId = ExternalId
            }};

            var ticketsToUpdate = new List<Ticket>();

            foreach (var ticket in tickets)
            {
                var createResp = await api.Tickets.CreateTicketAsync(ticket);
                ticketsToUpdate.Add(createResp.Ticket);
            }

            ticketsToUpdate.ForEach(t => t.Status = pendingStatus);

            var updateResp = await api.Tickets.BatchUpdateAsync(ticketsToUpdate);

            var job = await api.JobStatuses.GetJobStatusAsync(updateResp.JobStatus.Id);
            var count = 0;
            while (job.JobStatus.Status.ToLower() != "completed" && count < 10)
            {
                await Task.Delay(1000);
                job = await api.JobStatuses.GetJobStatusAsync(updateResp.JobStatus.Id);
                count++;
            }

            Assert.That(job.JobStatus.Status.ToLower(), Is.EqualTo("completed"));

            foreach (var r in job.JobStatus.Results)
            {
                var ticket = (await api.Tickets.GetTicketAsync(r.Id)).Ticket;
                Assert.That(ticket.Status, Is.EqualTo(pendingStatus));
                await api.Tickets.DeleteAsync(r.Id);
            }
        }
    }
}
