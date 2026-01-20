namespace SampleProject.Steps;

public class CalculatorSteps
{
    private int _result;
    private int _firstNumber;
    private int _secondNumber;

    [Given("I have entered (.*) into the calculator")]
    public void GivenIHaveEnteredIntoTheCalculator(int number)
    {
        _firstNumber = number;
    }

    [Given("I have also entered (.*) into the calculator")]
    public void GivenIHaveAlsoEnteredIntoTheCalculator(int number)
    {
        _secondNumber = number;
    }

    [When("I press add")]
    public void WhenIPressAdd()
    {
        _result = _firstNumber + _secondNumber;
    }

    [When("I press subtract")]
    public void WhenIPressSubtract()
    {
        _result = _firstNumber - _secondNumber;
    }

    [When("I press multiply")]
    public void WhenIPressMultiply()
    {
        _result = _firstNumber * _secondNumber;
    }

    [Then("the result should be (.*) on the screen")]
    public void ThenTheResultShouldBeOnTheScreen(int expected)
    {
        if (_result != expected)
            throw new Exception($"Expected {expected} but got {_result}");
    }

    [Then("the calculator should display an error")]
    public void ThenTheCalculatorShouldDisplayAnError()
    {
        // Error handling step
    }
}
