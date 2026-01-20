Feature: Calculator
  As a user
  I want to use a calculator
  So that I can perform basic arithmetic operations

  Scenario: Add two numbers
    Given I have entered 50 into the calculator
    And I have also entered 70 into the calculator
    When I press add
    Then the result should be 120 on the screen

  Scenario: Subtract two numbers
    Given I have entered 100 into the calculator
    And I have also entered 30 into the calculator
    When I press subtract
    Then the result should be 70 on the screen

  Scenario: Multiply two numbers
    Given I have entered 5 into the calculator
    And I have also entered 6 into the calculator
    When I press multiply
    Then the result should be 30 on the screen

  Scenario Outline: Basic arithmetic operations
    Given I have entered <first> into the calculator
    And I have also entered <second> into the calculator
    When I press <operation>
    Then the result should be <result> on the screen

    Examples:
      | first | second | operation | result |
      | 10    | 5      | add       | 15     |
      | 20    | 8      | subtract  | 12     |
      | 7     | 3      | multiply  | 21     |
