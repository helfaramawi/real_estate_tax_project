using Microsoft.AspNetCore.Mvc;
using RealEstateTax.Application.Common.Models;

namespace RealEstateTax.API.Extensions;

public static class ApiResponseExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
    {
        if (result.IsSuccess)
            return controller.Ok(new { success = true, data = result.Data });

        return result.ErrorCode switch
        {
            "NOT_FOUND" => controller.NotFound(new { success = false, errorCode = result.ErrorCode, message = result.Error }),
            "FORBIDDEN" => controller.StatusCode(403, new { success = false, errorCode = result.ErrorCode, message = result.Error }),
            "VALIDATION_ERROR" => controller.BadRequest(new { success = false, errorCode = result.ErrorCode, errors = result.ValidationErrors }),
            "DUPLICATE" => controller.Conflict(new { success = false, errorCode = result.ErrorCode, message = result.Error }),
            _ => controller.BadRequest(new { success = false, errorCode = result.ErrorCode, message = result.Error })
        };
    }

    public static IActionResult ToCreatedResult<T>(this Result<T> result, ControllerBase controller, string routeName, object routeValues)
    {
        if (!result.IsSuccess) return result.ToActionResult(controller);
        return controller.CreatedAtRoute(routeName, routeValues, new { success = true, data = result.Data });
    }
}
