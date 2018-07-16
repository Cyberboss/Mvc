using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Microsoft.AspNetCore.Mvc.Analyzers._OUTPUT_
{
    [ApiController]
    [ApiConventionType(typeof(ExtractToConvention_AddsNewConventionMethodToExistingConventionTypeConvention))]
    public class ExtractToConvention_AddsNewConventionMethodToExistingConventionType : ControllerBase
    {
        public ActionResult<ExtractToConvention_AddsNewConventionMethodToExistingConventionTypeModel> Get(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            return new ExtractToConvention_AddsNewConventionMethodToExistingConventionTypeModel();
        }

        public ActionResult<string> PostPerson(ExtractToConvention_AddsNewConventionMethodToExistingConventionTypeModel model)
        {
            if (!ModelState.IsValid)
            {
                return Conflict();
            }

            return CreatedAtAction(nameof(Get), model);
        }
    }

    public static class ExtractToConvention_AddsNewConventionMethodToExistingConventionTypeConvention
    {
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
        public static void Get(
            [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Suffix)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object id)
        { }

        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(409)]
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
        public static void Post([ApiConventionNameMatch(ApiConventionNameMatchBehavior.Suffix), ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)] object model)
        {
        }
    }

    public class ExtractToConvention_AddsNewConventionMethodToExistingConventionTypeModel { }
}
