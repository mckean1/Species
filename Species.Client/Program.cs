
using Species.Domain.Diagnostics;
using Species.Domain.Generation;
using Species.Domain.Validation;

var world = WorldGenerator.Create();
var validationErrors = WorldValidator.Validate(world);

if (validationErrors.Count > 0)
{
    Console.WriteLine("World generation failed validation:");

    foreach (var error in validationErrors)
    {
        Console.WriteLine($"- {error}");
    }

    return;
}

Console.WriteLine(WorldSummaryFormatter.Format(world));
