using System.Collections.Generic;

namespace API.Errors
{
    public class ApiValidationErrorResponose : ApiResponse
    {
        public ApiValidationErrorResponose() : base(400)
        {
        }
        
        public IEnumerable<string> Errors { get; set; }
    }
}