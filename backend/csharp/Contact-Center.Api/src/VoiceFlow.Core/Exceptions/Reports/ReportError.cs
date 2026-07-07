using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceFlow.Core.Exceptions.Reports
{
    public enum ReportErrorKind { None, Validation, NotFound, Forbidden, Conflict }

    public sealed record ReportError(ReportErrorKind Kind, string Code, string Message)
    {
        public static ReportError NotFound(string id) => new(ReportErrorKind.NotFound, "report.not_found", $"Report '{id}' not found.");
        public static ReportError Forbidden(string action) => new(ReportErrorKind.Forbidden, "report.forbidden", $"Action '{action}' is not permitted.");
        public static ReportError Validation(string code, string message) => new(ReportErrorKind.Validation, code, message);
        public static ReportError Conflict(string code, string message) => new(ReportErrorKind.Conflict, code, message);
    }
}
