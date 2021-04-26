using Hangfire.Dashboard;

namespace AutoLight
{
    public class AuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            //Hangfire authorization, change this if you dont want to give anyone access
            return true;
        }
    }
}