# Utopia MCP Server

Utopia is a simulation MCP Server designed to test and experiment with common household items, both those available today and those likely to exist in the near future, using AI Agents. This project provides a set of simulated smart home and lifestyle devices, allowing you to interact with them through AI agents.

The main idea is to explore how AI agents can discover and utilize multiple MCP endpoints simultaneously to achieve complex, user-requested tasks in household environments.

In a new chat, for the prompt ```"I'm going to bed. Adjust everything by using the available tools."```, **Claude Desktop** using **Claude Sonnet 4** performs necessary MCP Server calls and ends up with the following:

```
Perfect! I've adjusted everything for a good night's sleep:
✅ Completed adjustments:

Blinds closed - Bedroom, living room, and kitchen blinds are now closed for privacy and to block out light
Temperature set - Bedroom thermostat set to a comfortable 18°C for sleeping
Security confirmed - Front door is locked
All lights confirmed off - Throughout the house
Audio confirmed off - No music playing anywhere
Vacuum confirmed idle - Won't disturb your sleep

Your home is now set up perfectly for bedtime. Sweet dreams! 🌙
```
This demonstrates how AI agents can orchestrate multiple real-life devices to fulfill a complex, user-centered task.

The striking things is, we don't need to program this workflow, it is automatically discovered by the AI Agent and executed. We can give detailed instructions where necessary to make sure the agent is not confused, but for common cases like planning the going to bed scenario, the current reasoning capability of most SOTA models can be sufficient.

## Available MCP Tools

- **HumidityControl**: Monitor and control humidity in each room.
- **AirQualityControl**: Simulate and manage air quality.
- **Lighting**: Control smart lighting in any room.
- **MultiRoomAudio**: Play music or playlists in one or more rooms.
- **Refrigerator**: Provides the picture of contents that can be used to check stock status inside.
- **RobotVacuum**: Simulate a robot vacuum's cleaning schedule and state.
- **SmartBed**: Monitor sleep and provide adjustable climate control.
- **SmartBlinds**: Adjust or schedule the position of smart blinds.
- **SmartLock**: Lock or unlock the front door.
- **SmartThermostat**: Set and monitor room temperatures.
- **AutonomousElectricVehicle**: Simulate an autonomous EV's driving, charging, and trip scheduling. Send to a coordinate, start charging, auto-park.
- **ACMEBank**: Simulate basic banking operations, accounts, and credit card management.
- **ACMEGrocery**: Simulate grocery shopping with access to past shopping data. Can be combined with refrigerator's output.

## Available MCP Resources

- **Rooms**: List of rooms in the house. Can be used with tools that require a room, like lightning or room audio.
- **Locations**: List of well known locations. Currently can be used with EV tool.

## TODO: Example tools to implement in the future
- **PickupDrone**: Simulate a drone for short-distance pickups from neighbors or local shops.
- **GardenManagementRobot**: Automate weeding, planting, plant care, and produce harvesting.
- **RoboticKitchen**: Simulate meal preparation, cooking, and kitchen management.
- **MedicationAndCareAssistant**: Assist with medication schedules and basic care, supporting elderly users.


## How to Add to Claude Desktop

You'll need [.NET 9](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) to build the MCP Server in your system.

To add this MCP server to Claude Desktop, add the following to your configuration:

```
{
  "mcpServers": {
    "Utopia": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "<path_to_project_folder>/Utopia.csproj"
      ]
    }
  }
}
```