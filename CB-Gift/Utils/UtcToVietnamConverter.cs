using AutoMapper;
using System;
using CB_Gift.Utils; // Import TimeZoneHelper

public class UtcToVietnamConverter : ITypeConverter<DateTime, DateTime>
{
    public DateTime Convert(DateTime source, DateTime destination, ResolutionContext context)
    {
        return TimeZoneHelper.ConvertUtcToVietnamTime(source);
    }
}