# Anti Gravity Engine for VRChat Worlds

This is an advanced player/pickupable gimmicks physics system.
The main focus of this system is for making illusions and some impossible gravity which can't be achieved with normal approach.

This system can't be used standalone and it might requires some UdonSharp scripting to make it works.
Please refer to the example worlds on how you implement your world's physics.

## Getting Started

0. To install this package, you should use [VRChat Creator Companion](https://vcc.docs.vrchat.com/) with [my package listings](https://xtlcdn.github.io/vpm/).
1. Click on `GameObject > Anti Gravity Engine > Anti Gravity Manager` in menu to add the manager to your scene.
   Each world only require exactly one of this manager to work, even you want to make multiple contraptions that will make use of this system.
2. Depends on what you want to achieve, you can use following scripts for pre-made behaviours.
   Just add them to your reference transform (root) game object
   (Beware you can't use the AGE Manager object you have added previously as root, as this may produces undefined behaviour!).
   - **SphericalGroundHandler**: Make your world has a spherical ground surface instead of flat.
     Beware this may amplify VR motion sickness for visitors.  
     You may find the sample implementation in "Demo1 - Sphere" scene.
   - **TileLooper**: The most basic non euclidean geometry, which you can appears from another edge of a tile when you go across it.
     You may find the sample implementation in "Demo2 - RPG-Like Infinity Scroller" scene.
3. If above are not exactly what you want, you can make it your own!
   1. Right-click inside a folder where you want to put your scripts in project tab,
      select `Create > Anti Gravity Engine > Custom Anti Gravity Handler`,
      and you will prompted to create a new script. Just do it like how you create other U# scripts.
   2. You can now start working with the template.
   3. Create a new game object and add this newly created script as a component.
4. Go to the ACE Manager you added at step 1, assign *ALL* your handlers to here, that is what you have created at step 2/3.
5. Depends on your design, you can configurate these settings:
   - **Auto Reattach**: When user try to leave the system, re-attach them back.
   - **Auto Use On Login**: When user joins, they will be teleported to your contraption. Useful if your world doesn't have a "lobby" space.
   - **Detech On Respawn**: This is the opposite of above options, when user respawns, they will leave the contraption.
6. If you have pickupable objects (no matter local or global) that needed to work with AGE,
   remove "VRC Object Sync" if there is any of those pickups and then add "Anti Gravity Object Sync" instead.  
   When an object has a configurated "Anti Gravity Object Sync" component,
   it will follow the geometry of your contraption just like other users do.  
   By default, it is not configurated to use any handlers, so if you want it attached initially,
   you will have to assign your contraption to "Initial Selected Handler".
7. If you have advanced usage such as users only get on your contraption when required/requested,
   and want to control it programmatically, you may refer to `<summary>` inside comments on each components for details.
8. Go to test it! Even you should have an outstanding world with non-euclidean geometry and/or weird physics now, but testing is important. Please spawn at least 2 VRChat clients to test if it is behaving what you want it to!

## But... Wait! I want to see it in action!

There are already several worlds are utilizing this system.
- [Infinity Staircase](https://vrchat.com/home/world/wrld_b06797a2-801e-4a9a-a49d-1eb8ed06e031), the physically impossible part is implemented using AGE.
- [3D Maze Screensaver](https://vrchat.com/home/world/wrld_c259b81a-405a-46be-8b56-0b731992e4c4), pretty good at flipping players upside down.
- [The Anarchy Stronghold](https://vrchat.com/home/world/wrld_92f7c812-c14e-41c6-9f0a-8de1a04cd48b), on how to achieve infinite hallway which can meet back your friends in a loop.
- [Debug Jinja](https://vrchat.com/home/world/wrld_61f2fa45-e023-48dd-8c69-fdb658da9347), in a secret gimmick, you will know it once you found it.

## License

[MIT](LICENSE)