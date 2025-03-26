using System.Reflection;
using API.Extensions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace API.Data;

public class StringUnescapeInterceptor:IMaterializationInterceptor
{
    public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
    {
        return entity;

    }
}