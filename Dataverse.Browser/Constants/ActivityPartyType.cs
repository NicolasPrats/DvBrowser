using System;
using System.Collections.Generic;
using System.IdentityModel.Protocols.WSTrust;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dataverse.Browser.Properties;

namespace Dataverse.Browser.Constants
{
    //https://learn.microsoft.com/en-us/power-apps/developer/data-platform/activityparty-entity#activity-party-types
    internal static class ActivityPartyType
    {
        public const int Sender = 1;// Specifies the sender.
        public const int ToRecipient = 2;// Specifies the recipient in the To field.
        public const int CCRecipient = 3;// = Specifies the recipient in the Cc field.
        public const int BccRecipient = 4;// = Specifies the recipient in the Bcc field.
        public const int RequiredAttendee = 5;// = Specifies a required attendee.
        public const int OptionalAttendee = 6;// = Specifies an optional attendee.
        public const int Organizer = 7;// = Specifies the activity organizer.
        public const int Regarding = 8;// = Specifies the regarding item.
        public const int Owner = 9;// = Specifies the activity owner.
        public const int Resource = 10;// = Specifies a resource.
        public const int Customer = 11;// = Specifies a customer.
        public const int ChatParticipant = 12;// = Specifies a participant in a Teams chat.
    }
}
