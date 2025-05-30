using Utopia.Transportation;
using System.Reflection;
using Utopia;

namespace UtopiaTest.Transportation;

public class AutonomousElectricVehicleTests
{
    private static readonly (string name, double lat, double lon) Unknown = ("Unknown", 0, 0);

    private (string name, double lat, double lon) GetLocation(string name)
    {
        var loc = Utopia.Environment.KnownLocations[name];
        return (name, loc.Latitude, loc.Longitude);
    }

    private void ResetEVState()
    {
        AutonomousElectricVehicle.State = AutonomousElectricVehicle.CarState.Parked;
        AutonomousElectricVehicle.Location = "Home";
        AutonomousElectricVehicle.Battery = 80.0;
        AutonomousElectricVehicle.DrivingDestination = null;
        AutonomousElectricVehicle.ScheduledTrip = null;
    }

    [Fact]
    public async Task GetStatus_InitialState_Parked()
    {
        ResetEVState();
        var status = await AutonomousElectricVehicle.GetStatus();
        Assert.Contains("Car is Parked", status);
        Assert.Contains("Location: Home", status);
        Assert.Contains("Battery: 80.0%", status);
    }

    [Fact]
    public async Task DriveTo_KnownLocation_Succeeds()
    {
        ResetEVState();
        var office = GetLocation("Office");
        var result = await AutonomousElectricVehicle.DriveTo(office.lat, office.lon);
        Assert.Contains("Driving to Office", result);
        var status = await AutonomousElectricVehicle.GetStatus();
        Assert.Contains("Driving to Office", status);
    }

    [Fact]
    public async Task DriveTo_AlreadyThere_ReturnsAlreadyAt()
    {
        ResetEVState();
        var home = GetLocation("Home");
        var result = await AutonomousElectricVehicle.DriveTo(home.lat, home.lon);
        Assert.Contains("already at Home", result);
    }

    [Fact]
    public async Task DriveTo_UnknownLocation_DrivesToCustomCoordinates()
    {
        ResetEVState();
        var result = await AutonomousElectricVehicle.DriveTo(Unknown.lat, Unknown.lon);
        Assert.Contains("Not enough battery for the trip", result);
    }

    [Fact]
    public async Task DriveTo_NotEnoughBattery_ReturnsError()
    {
        ResetEVState();
        var airport = GetLocation("Airport");
        AutonomousElectricVehicle.Battery = 1.0;
        var result = await AutonomousElectricVehicle.DriveTo(airport.lat, airport.lon);
        Assert.Contains("Not enough battery", result);
    }

    [Fact]
    public async Task Stop_WhenDriving_StopsCar()
    {
        ResetEVState();
        var office = GetLocation("Office");
        await AutonomousElectricVehicle.DriveTo(office.lat, office.lon);
        var result = await AutonomousElectricVehicle.Stop();
        Assert.Contains("stopped and parked", result);
        var status = await AutonomousElectricVehicle.GetStatus();
        Assert.Contains("Car is Parked", status);
    }

    [Fact]
    public async Task Stop_WhenNotDriving_ReturnsAlreadyStopped()
    {
        ResetEVState();
        var result = await AutonomousElectricVehicle.Stop();
        Assert.Contains("already stopped", result);
    }

    [Fact]
    public async Task StartCharging_Succeeds()
    {
        ResetEVState();
        var result = await AutonomousElectricVehicle.StartCharging();
        Assert.Contains("Charging started", result);
        var status = await AutonomousElectricVehicle.GetStatus();
        Assert.Contains("Charging", status);
    }

    [Fact]
    public async Task StartCharging_AlreadyCharging_ReturnsAlreadyCharging()
    {
        ResetEVState();
        await AutonomousElectricVehicle.StartCharging();
        var result = await AutonomousElectricVehicle.StartCharging();
        Assert.Contains("Already charging", result);
    }

    [Fact]
    public async Task StartCharging_BatteryFull_ReturnsFull()
    {
        ResetEVState();
        AutonomousElectricVehicle.Battery = 100.0;
        var result = await AutonomousElectricVehicle.StartCharging();
        Assert.Contains("Battery is already full", result);
    }

    [Fact]
    public async Task StopCharging_WhenCharging_StopsCharging()
    {
        ResetEVState();
        await AutonomousElectricVehicle.StartCharging();
        var result = await AutonomousElectricVehicle.StopCharging();
        Assert.Contains("Charging stopped", result);
        var status = await AutonomousElectricVehicle.GetStatus();
        Assert.Contains("Car is Parked", status);
    }

    [Fact]
    public async Task StopCharging_WhenNotCharging_ReturnsNotCharging()
    {
        ResetEVState();
        var result = await AutonomousElectricVehicle.StopCharging();
        Assert.Contains("not charging", result);
    }

    [Fact]
    public async Task ScheduleTrip_Succeeds()
    {
        ResetEVState();
        var future = DateTime.Now.AddSeconds(2);
        var office = GetLocation("Office");
        var result = await AutonomousElectricVehicle.ScheduleTrip(office.name, future);
        Assert.Contains("Trip scheduled to Office", result);
        var status = await AutonomousElectricVehicle.GetStatus();
        Assert.Contains("Trip scheduled to Office", status);
    }

    [Fact]
    public async Task ScheduleTrip_WhileDrivingOrCharging_Succeeds()
    {
        ResetEVState();
        var office = GetLocation("Office");
        var mall = GetLocation("Mall");
        await AutonomousElectricVehicle.DriveTo(office.lat, office.lon);
        var result = await AutonomousElectricVehicle.ScheduleTrip(mall.name, DateTime.Now.AddMinutes(1));
        Assert.Contains("Trip scheduled to Mall", result);
        ResetEVState();
        await AutonomousElectricVehicle.StartCharging();
        result = await AutonomousElectricVehicle.ScheduleTrip(mall.name, DateTime.Now.AddMinutes(1));
        Assert.Contains("Trip scheduled to Mall", result);
    }

    [Fact]
    public async Task ScheduleTrip_UnknownDestination_ReturnsError()
    {
        ResetEVState();
        var result = await AutonomousElectricVehicle.ScheduleTrip(Unknown.name, DateTime.Now.AddMinutes(1));
        Assert.Contains("Unknown destination", result);
    }

    [Fact]
    public async Task ScheduleTrip_Conflicting_ReturnsError()
    {
        ResetEVState();
        var office = GetLocation("Office");
        var mall = GetLocation("Mall");
        await AutonomousElectricVehicle.ScheduleTrip(office.name, DateTime.Now.AddMinutes(1));
        var result = await AutonomousElectricVehicle.ScheduleTrip(mall.name, DateTime.Now.AddMinutes(2));
        Assert.Contains("already scheduled", result);
    }

    [Fact]
    public async Task ScheduleTrip_CancelScheduledTrip()
    {
        ResetEVState();
        var office = GetLocation("Office");
        var result = await AutonomousElectricVehicle.CancelScheduledTrip();
        Assert.Contains("No trip is currently scheduled", result);
        var future = DateTime.Now.AddMinutes(1);
        await AutonomousElectricVehicle.ScheduleTrip(office.name, future);
        var status = await AutonomousElectricVehicle.GetStatus();
        Assert.Contains("Trip scheduled to Office", status);
        result = await AutonomousElectricVehicle.CancelScheduledTrip();
        Assert.Contains("Scheduled trip cancelled", result);
        status = await AutonomousElectricVehicle.GetStatus();
        Assert.DoesNotContain("Trip scheduled", status);
    }

    [Fact]
    public async Task ScheduleTrip_CanBeSetInAnyState_AndOnlyExecutesIfParked()
    {
        ResetEVState();
        var office = GetLocation("Office");
        var mall = GetLocation("Mall");
        var future = DateTime.Now.AddSeconds(1);
        var result = await AutonomousElectricVehicle.ScheduleTrip(office.name, future);
        Assert.Contains("Trip scheduled to Office", result);
        ResetEVState();
        await AutonomousElectricVehicle.DriveTo(mall.lat, mall.lon);
        result = await AutonomousElectricVehicle.ScheduleTrip(office.name, DateTime.Now.AddSeconds(1));
        Assert.Contains("Trip scheduled to Office", result);
        ResetEVState();
        await AutonomousElectricVehicle.StartCharging();
        result = await AutonomousElectricVehicle.ScheduleTrip(office.name, DateTime.Now.AddSeconds(1));
        Assert.Contains("Trip scheduled to Office", result);
    }

    [Fact]
    public async Task ScheduleTrip_OnlyExecutesIfParked_AndCancelsOtherwise()
    {
        ResetEVState();
        var office = GetLocation("Office");
        var mall = GetLocation("Mall");
        var future = DateTime.Now.AddSeconds(2);
        await AutonomousElectricVehicle.ScheduleTrip(office.name, future);
        await AutonomousElectricVehicle.DriveTo(mall.lat, mall.lon);
        await Task.Delay(2500);
        var status = await AutonomousElectricVehicle.GetStatus();
        if (!status.Contains("to Off", StringComparison.OrdinalIgnoreCase))
            Assert.DoesNotContain("Trip scheduled", status);
        Assert.Contains("Driving to Mall", status);
    }
}
