# Anti Gravity Engine for VRChat Worlds

This is an advanced player/pickupable gimmicks physics system. The main focus of this system is for making illusions and some impossible gravity which can't be achieved with normal approach.

This system can't be used standalone and it requires some UDON scripting to make it works (especially on the logic to calculate players' appearing position and orientation). Please refer to the example worlds on how you implement your world's physics.

## Getting Started

To install this package, you should use [VRChat Creator Companion](https://vcc.docs.vrchat.com/). Step by step details TBD.

There are 2 demo scenes, you may refer them to start build your own world.
There is also a template file named `AntiGravityCustomHandlerTemplate.cs`, you can use it as a "clean" template to implement custom physics logic.

## But... Wait! I want to see it in action!

There are already several worlds are utilizing this system.
- [Infinity Staircase](https://vrchat.com/home/world/wrld_b06797a2-801e-4a9a-a49d-1eb8ed06e031), the physically impossible part is implemented using AGE.
- [3D Maze Screensaver](https://vrchat.com/home/world/wrld_c259b81a-405a-46be-8b56-0b731992e4c4), pretty good at flipping players upside down.
- [The Anarchy Stronghold](https://vrchat.com/home/world/wrld_92f7c812-c14e-41c6-9f0a-8de1a04cd48b), on how to achieve infinite hallway which can meet back your friends in a loop.
- [Debug Jinja](https://vrchat.com/home/world/wrld_61f2fa45-e023-48dd-8c69-fdb658da9347), in a secret gimmick, you will know it once you found it.

## License

[MIT](LICENSE)