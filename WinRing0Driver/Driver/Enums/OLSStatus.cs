﻿//-----------------------------------------------------------------------------
//     Author : hiyohiyo
//       Mail : hiyohiyo@crystalmark.info
//        Web : http://openlibsys.org/
//    License : The modified BSD license
//
//                     Copyright 2007-2009 OpenLibSys.org. All rights reserved.
//-----------------------------------------------------------------------------
// This is support library for WinRing0 1.3.x.
//
// Code adjustments and additions by Florian K.

namespace WinRing0Driver.Driver.Enums
{
    //For this support library
    public enum OLSStatus
    {
        NO_ERROR               = 0,
        DLL_NOT_FOUND          = 1,
        DLL_INCORRECT_VERSION  = 2,
        DLL_INITIALIZE_ERROR   = 3,
        DLL_RESOURCE_NOT_FOUND = 4,
    }
}
