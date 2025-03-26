namespace API.MiddleWare;
using System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class IgnoreCsrfValidationAttribute : Attribute { };

