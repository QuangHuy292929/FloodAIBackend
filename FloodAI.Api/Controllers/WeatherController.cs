using System.ComponentModel.DataAnnotations;
using FloodAI.Business.Weather;
using Microsoft.AspNetCore.Mvc;

namespace FloodAI.Api.Controllers;

[ApiController]
[Route("api/v1/weather")]
public sealed class WeatherController(WeatherService weather) : ControllerBase
{
    [HttpGet("current")]
    [ProducesResponseType<CurrentWeather>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public Task<CurrentWeather> GetCurrent(
        [FromQuery, Required, Range(typeof(long), "1", "9223372036854775807")] long? areaId,
        CancellationToken cancellationToken) =>
        weather.GetCurrentAsync(areaId!.Value, cancellationToken);
}
