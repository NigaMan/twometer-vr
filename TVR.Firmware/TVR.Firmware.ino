#include <ESP8266WiFi.h>

#include "IO/ButtonInput.h"
#include "IO/PoseInput.h"
#include "Model/TrackerClass.h"
#include "Model/TrackerColor.h"
#include "Utils/Constants.h"
#include "Utils/SerialNo.h"
#include "Net/NetDefs.h"
#include "Net/Discovery.h"
#include "Net/UdpClient.h"

const TrackerClass trackerClass = TrackerClass::Controller;
const TrackerColor trackerColor = TrackerColor::Red;

ButtonInput buttonInput;
PoseInput poseInput;
UdpClient client;

uint8_t trackerId;

void setup()
{
    Serial.begin(115200);
    Serial.println("");
    Serial.println(VERISON_STRING);

    Logger::info("Setting up hardware...");
    buttonInput.begin();
    poseInput.begin();

    // TODO connect to WiFi here

    Logger::info("Discovering server...");
    Discovery discovery;
    IPAddress serverIp = discovery.discover();
    client.begin(serverIp, UNICAST_PORT);

    Logger::info("Registering with the server...");
    Packets::sendHello(client, trackerClass, trackerColor, SerialNo::get());
    Packets::receiveHello(client, trackerId);
    Serial.printf("Tracker id: %d\n", trackerId);
    Logger::info("Successfully registered with the server.");
}

void loop()
{
    buttonInput.update();
    poseInput.update();

    if (poseInput.available())
    {
        Packets::sendState(client, trackerId, buttonInput.getStates(), poseInput.getPose());
        poseInput.clearAvailable();
    }
}
