﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace DevToys.Blazor.BuiltInDataTypeDetectors;

[Export(typeof(IDataTypeDetector))]
[DataTypeName(PredefinedCommonDataTypeNames.Json, baseName: PredefinedCommonDataTypeNames.Text)]
internal sealed partial class JsonDataTypeDetector : IDataTypeDetector
{
    private readonly ILogger _logger;

    [ImportingConstructor]
    public JsonDataTypeDetector()
    {
        _logger = this.Log();
    }

    public async ValueTask<DataDetectionResult> TryDetectDataAsync(
        object data,
        DataDetectionResult? resultFromBaseDetector,
        CancellationToken cancellationToken)
    {
        if (resultFromBaseDetector is not null
            && resultFromBaseDetector.Data is string dataString
            && !long.TryParse(dataString, out _))
        {
            if (await IsValidAsync(dataString, _logger, cancellationToken))
            {
                return new DataDetectionResult(Success: true, Data: dataString);
            }
        }

        return DataDetectionResult.Unsuccessful;
    }

    /// <summary>
    /// Detects whether the given string is a valid JSON or not.
    /// </summary>
    internal static async ValueTask<bool> IsValidAsync(string? input, ILogger logger, CancellationToken cancellationToken)
    {
        if (input == null)
        {
            return true;
        }

        if (long.TryParse(input, out _))
        {
            return false;
        }

        try
        {
            using JsonReader reader = new JsonTextReader(new StringReader(input));
            JToken? jtoken = await JToken.LoadAsync(reader, settings: null, cancellationToken);
            return jtoken is not null;
        }
        catch (JsonReaderException)
        {
            // Exception in parsing json. It likely mean the text isn't a JSON.
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Invalid data detected.");
            return false;
        }
    }
}
