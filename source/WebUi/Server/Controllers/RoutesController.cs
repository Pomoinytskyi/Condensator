using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace WebUi.Server.Controllers;

[Route("routes")]
[ApiController]
public class RoutesController : ControllerBase
{
    private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;

    public RoutesController(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
    {
        _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
    }

    public RoutesResultModel Get()
    {
        var routes = _actionDescriptorCollectionProvider.ActionDescriptors.Items.Where(
            ad => ad.AttributeRouteInfo != null).Select(ad => new RouteModel
        {
            Name = ad.AttributeRouteInfo.Template,
            Method = ad.ActionConstraints?.OfType<HttpMethodActionConstraint>().FirstOrDefault()?.HttpMethods.First(),
            }).ToList();

        var res = new RoutesResultModel
        {
            Routes = routes
        };

        return res;
    }
}
