using System.Text.Json;
using Nocturne.Connectors.MyLife.Constants;
using Nocturne.Connectors.MyLife.Mappers.Helpers;
using Nocturne.Connectors.MyLife.Models;
using Nocturne.Core.Models;

namespace Nocturne.Connectors.MyLife.Mappers.Handlers;

internal sealed class ProfileSwitchTreatmentHandler : IMyLifeTreatmentHandler
{
    public bool CanHandle(MyLifeEvent ev)
    {
        if (ev.EventTypeId != MyLifeEventTypeIds.Indication)
        {
            return false;
        }

        var info = MyLifeMapperHelpers.ParseInfo(ev.InformationFromDevice);
        if (info == null)
        {
            return false;
        }

        if (!info.Value.TryGetProperty(MyLifeJsonKeys.Key, out var keyElement))
        {
            return false;
        }

        if (keyElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var key = keyElement.GetString();
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (string.Equals(key, MyLifeJsonKeys.IndicationBasalProfileXChanged, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(key, MyLifeJsonKeys.IndicationBasalProfileChanged, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public IEnumerable<Treatment> Handle(MyLifeEvent ev, MyLifeTreatmentContext context)
    {
        var info = MyLifeMapperHelpers.ParseInfo(ev.InformationFromDevice);
        var profileSwitch = MyLifeTreatmentFactory.CreateWithSuffix(
            ev,
            MyLifeTreatmentTypes.ProfileSwitch,
            MyLifeIdSuffixes.ProfileSwitch
        );
        profileSwitch.Notes = ev.InformationFromDevice;

        if (info != null)
        {
            if (info.Value.TryGetProperty(MyLifeJsonKeys.Key, out var keyElement))
            {
                if (keyElement.ValueKind == JsonValueKind.String)
                {
                    var key = keyElement.GetString();
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        if (string.Equals(
                                key,
                                MyLifeJsonKeys.IndicationBasalProfileXChanged,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            if (info.Value.TryGetProperty(MyLifeJsonKeys.Parameter0, out var profileElement))
                            {
                                if (profileElement.ValueKind == JsonValueKind.String)
                                {
                                    var profile = profileElement.GetString();
                                    if (!string.IsNullOrWhiteSpace(profile))
                                    {
                                        profileSwitch.Profile = profile;
                                    }
                                }
                            }
                        }

                        if (string.Equals(
                                key,
                                MyLifeJsonKeys.IndicationBasalProfileChanged,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            if (info.Value.TryGetProperty(MyLifeJsonKeys.Parameter1, out var profileElement))
                            {
                                if (profileElement.ValueKind == JsonValueKind.String)
                                {
                                    var profile = profileElement.GetString();
                                    if (!string.IsNullOrWhiteSpace(profile))
                                    {
                                        profileSwitch.Profile = profile;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return new List<Treatment>
        {
            profileSwitch
        };
    }
}
