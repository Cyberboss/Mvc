using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Microsoft.AspNetCore.Mvc.Analyzers._INPUT_
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

        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    }

    public class ExtractToConvention_AddsNewConventionMethodToExistingConventionTypeModel { }
}
