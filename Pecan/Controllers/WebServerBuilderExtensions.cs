using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Pecan.Controllers
{
    public static class WebServerBuilderExtensions
    {
        public static WebServerBuilder MapController<T>(this WebServerBuilder builder, T controller) where T : class
        {
            var methods = typeof(T).GetMethods();

            foreach (MethodInfo method in methods)
            {
                if (method.IsStatic || !method.IsPublic)
                {
                    continue;
                }

                var attributes = method.GetCustomAttributes<HttpMethodAttribute>();

                if (attributes == null)
                {
                    continue;
                }

                var parameters = method.GetParameters();

                foreach (var attr in attributes)
                {
                    if (parameters.Length > 1)
                    {
                        throw new InvalidOperationException("No mapping to method parameters for " + method.Name);
                    }
                    else if (parameters.Length == 1 && parameters[0].ParameterType != typeof(HttpListenerContext))
                    {
                        throw new InvalidOperationException("No mapping to method parameter for" + method.Name);
                    }

                    if (method.ReturnType == typeof(void))
                    {
                        Action<HttpListenerContext> expr = (HttpListenerContext context) =>
                            method.Invoke(controller, parameters.Length == 0 ? null : new object[] { context });

                        builder.Map("/" + method.Name, attr.HttpMethod, expr);
                    }
                    else if (method.ReturnType == typeof(Task))
                    {
                        Func<HttpListenerContext, Task> expr = (HttpListenerContext context) =>
                            (Task)method.Invoke(controller, parameters.Length == 0 ? null : new object[] { context });

                        builder.Map("/" + method.Name, attr.HttpMethod, expr);
                    }
                    else if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        var resultProperty = method.ReturnType.GetProperty("Result");

                        Func<HttpListenerContext, Task<object>> expr = async (HttpListenerContext context) =>
                        {
                            var task = (Task)method.Invoke(controller, parameters.Length == 0 ? null : new object[] { context });
                            await task
                                .ConfigureAwait(false);

                            return resultProperty.GetValue(task);
                        };

                        builder.Map("/" + method.Name, attr.HttpMethod, expr);
                    }
                    else
                    {
                        Func<HttpListenerContext, object> expr = (HttpListenerContext context) =>
                            method.Invoke(controller, parameters.Length == 0 ? null : new object[] { context });

                        builder.Map("/" + method.Name, attr.HttpMethod, expr);
                    }
                }
            }

            return builder;
        }
    }
}
