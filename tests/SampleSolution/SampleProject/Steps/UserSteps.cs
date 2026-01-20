namespace SampleProject.Steps;

// Simulated Reqnroll/SpecFlow attributes for testing
[AttributeUsage(AttributeTargets.Method)]
public class GivenAttribute : Attribute
{
    public string Pattern { get; }
    public GivenAttribute(string pattern) => Pattern = pattern;
}

[AttributeUsage(AttributeTargets.Method)]
public class WhenAttribute : Attribute
{
    public string Pattern { get; }
    public WhenAttribute(string pattern) => Pattern = pattern;
}

[AttributeUsage(AttributeTargets.Method)]
public class ThenAttribute : Attribute
{
    public string Pattern { get; }
    public ThenAttribute(string pattern) => Pattern = pattern;
}

[AttributeUsage(AttributeTargets.Method)]
public class AndAttribute : Attribute
{
    public string Pattern { get; }
    public AndAttribute(string pattern) => Pattern = pattern;
}

[AttributeUsage(AttributeTargets.Method)]
public class ButAttribute : Attribute
{
    public string Pattern { get; }
    public ButAttribute(string pattern) => Pattern = pattern;
}

public class UserSteps
{
    [Given("a user with name (.*)")]
    public void GivenAUserWithName(string name)
    {
        // Step implementation
    }

    [Given("the user is logged in")]
    public void GivenTheUserIsLoggedIn()
    {
        // Step implementation
    }

    [When("the user clicks the login button")]
    public void WhenTheUserClicksTheLoginButton()
    {
        // Step implementation
    }

    [When("the user enters password (.*)")]
    public void WhenTheUserEntersPassword(string password)
    {
        // Step implementation
    }

    [Then("the user should see the dashboard")]
    public void ThenTheUserShouldSeeTheDashboard()
    {
        // Step implementation
    }

    [Then("the user should receive a welcome message")]
    public void ThenTheUserShouldReceiveAWelcomeMessage()
    {
        // Step implementation
    }

    [And("the user has admin privileges")]
    public void AndTheUserHasAdminPrivileges()
    {
        // Step implementation
    }

    [But("the user is not banned")]
    public void ButTheUserIsNotBanned()
    {
        // Step implementation
    }
}
