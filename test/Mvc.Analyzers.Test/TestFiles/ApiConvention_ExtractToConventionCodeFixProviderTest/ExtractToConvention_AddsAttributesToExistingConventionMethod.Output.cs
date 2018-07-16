using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Microsoft.AspNetCore.Mvc.Analyzers._OUTPUT_
{
    [ApiController]
    [ApiConventionType(typeof(ExtractToConvention_AddsAttributesToExistingConventionMethodConvention))]
    public class ExtractToConvention_AddsAttributesToExistingConventionMethod : ControllerBase
    {
        public ActionResult<string> GetPerson(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }
            else if (!User.IsInRole("SuperAdmin"))
            {
                return Unauthorized();
            }

            return string.Empty;
        }
    }

    public static class ExtractToConvention_AddsAttributesToExistingConventionMethodConvention
    {
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
        [ProducesResponseType(401)]
        public static void Get(
            [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Suffix)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object id)
        { }

        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
        public static void Post(
            [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object model)
        { }
    }
}
