Feature: User Login
  As a registered user
  I want to be able to login to the system
  So that I can access my account

  Background:
    Given the user is on the login page

  Scenario: Successful login with valid credentials
    Given a user with name "john.doe"
    And the user has admin privileges
    When the user enters password "secret123"
    And the user clicks the login button
    Then the user should see the dashboard
    And the user should receive a welcome message

  Scenario: Failed login with invalid password
    Given a user with name "john.doe"
    When the user enters password "wrongpassword"
    And the user clicks the login button
    Then the user should see an error message

  Scenario: Login attempt with banned user
    Given a user with name "banned.user"
    But the user is not banned
    When the user clicks the login button
    Then the user should see the dashboard

  Scenario Outline: Login with different user types
    Given a user with name "<username>"
    When the user enters password "<password>"
    And the user clicks the login button
    Then the user should see the dashboard

    Examples:
      | username  | password  |
      | admin     | admin123  |
      | moderator | mod456    |
      | guest     | guest789  |
