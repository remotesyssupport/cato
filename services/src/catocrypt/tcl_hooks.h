//########################################################################
// Copyright 2011 Cloud Sidekick
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//########################################################################

#ifndef __TCL_HOOKS_H__
#define __TCL_HOOKS_H__

#include <tcl.h>

int tcl_encryptPassword(ClientData clientData, Tcl_Interp *interp, int objc, Tcl_Obj * CONST objv[]);
int tcl_decryptPassword(ClientData clientData, Tcl_Interp *interp, int objc, Tcl_Obj * CONST objv[]);

#endif // #define __TCL_HOOKS_H__
