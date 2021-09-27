# AutoLight
AutoLight is an extremely simple Web API written in C# and .NET Core 5 to automatically turn on a light at sunset (-1 hour because of preference) through Home Assistant.

Uses Hangfire to schedule the task of turning on/off.

I made this so I can "clock" into my room with an NFC tag and make sure the light turns on at sunset so I don't have to myself.
Clocking out also turns off the light.
