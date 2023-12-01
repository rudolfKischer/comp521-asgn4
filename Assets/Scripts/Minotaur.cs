using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Minotaur : MonoBehaviour
{
    /*
    * The minotaur needs to accomplish the following:
    * 1. Gaurd the treasure, whic means it must be around it when it can
    * 2. Chases players within a certain radius
    * 3. Have an Idle behaviour when not doing any thing
    * 4. Needs to be able to follow and attack players, prioritised in this order:
    *     -  Who is currently attacking the minotaur most recently
    *     -  Who is closest to the treasure
    *     -  who is closest to the minotaur
    * 5. Needs to be able to attack players using an area of effect attack
    */     

    // Default state is gaurding the treasure
    // we will walk around the treasure in a circle

    // if we are attacked we pursue an attacker
    // we continue to purse this attacker until the following conditions are met:
    // 1. The attacker is dead
    // 2. The attacker is out of the treasure
    

    // if we are not currently being attacked, and there is a player within
    // a certain radius of the treasure, we will pursue them
    // we should pursue the player closest to the treasure
    // same conditions as above apply

    // if we are not currently being attacked, and there is no player within
    // if no one is within a radius of the treasure
    // and someone is close to us, we will pursure them
    // if another player starts attacking us, we will switch to them
    // if no one is attacking us, we will return to gaurding the treasure
    // if someone enters the treasure radius, we will switch to them


    
}
