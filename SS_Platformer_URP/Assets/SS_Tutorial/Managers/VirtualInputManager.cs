﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ss_tutorial
{
    public class VirtualInputManager : Singleton<VirtualInputManager>
    {
        public bool MoveRight;
        public bool MoveLeft;
        public bool Jump;
        public bool Attack;
    }

}