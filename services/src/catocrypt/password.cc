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

#include <tcl.h>
#include <string.h>
#include <stdlib.h>
#include "blowfish.h"

using namespace std;

int
tcl_encryptPassword(ClientData clientData, Tcl_Interp *interp, int objc,
            Tcl_Obj * CONST objv[])
{
    char *pword = NULL;
    char *key = NULL;
    static char key_string[32];
    string ePassword;
    if (objc != 3) {
        Tcl_SetResult(interp, (char *)"ERROR: encrypt requires exactly two arguments:string_to_encrypt and key", TCL_VOLATILE);
        return TCL_ERROR;
    }
    pword = strdup((const char *)objv[1]);
    if (!pword) {
        Tcl_SetResult(interp, (char *)"ERROR: Unable to retrieve password from params.", TCL_VOLATILE);
        return TCL_ERROR;
    }
    key = strdup((const char *)objv[2]);
    if (strlen(key) == 0) {
	/* 
		To customize the secret encryption key, change the following line
		and the same line in the decrypt function and recompile.
		NOTE: if this key is changed AFTER installation, any 
		previously encrypted values will be worthless. 
	*/
	sprintf(key_string,"%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c",83,116,89,87,76,99,65,50,115,87,97,112,72,76,66,109,115,78,101,86,53,89,88,69,69,85,101,90,107,75,81,77);
	key = strdup((const char *)key_string);
    }

    if (!encryptPassword(pword, ePassword, key)) {
        Tcl_SetResult(interp, (char *)"ERROR: Failed to encrypt password.", TCL_VOLATILE);
        return TCL_ERROR;
    }

    Tcl_SetResult(interp, (char *)ePassword.c_str(), TCL_VOLATILE);
    free(pword);
    free(key);
    return TCL_OK;
}

int
tcl_decryptPassword(ClientData clientData, Tcl_Interp *interp, int objc,
            Tcl_Obj * CONST objv[])
{
    char *ePword = NULL;
    char *key = NULL;
    static char key_string[32];
    string pword;
    if (objc != 3) {
        Tcl_SetResult(interp, (char *)"ERROR: decrypt requires exactly two arguments:string_to_decrypt and key", TCL_VOLATILE);
        return TCL_ERROR;
    }

    ePword = strdup((const char *)objv[1]);
    key = strdup((const char *)objv[2]);
    if (strlen(key) == 0) {
        /* 
                To customize the secret encryption key, change the following line
                and the same line in the encrypt function and recompile.
                NOTE: if this key is changed AFTER installation, any 
                previously encrypted values will be worthless. 
        */
	sprintf(key_string,"%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c%c",83,116,89,87,76,99,65,50,115,87,97,112,72,76,66,109,115,78,101,86,53,89,88,69,69,85,101,90,107,75,81,77);
	key = strdup((const char *)key_string);
    }
    if (!ePword) {
        Tcl_SetResult(interp, (char *)"ERROR: Unable to retrieve encrypted password from params.", 
                TCL_VOLATILE);
        return TCL_ERROR;
    }

    if (!decryptPassword(ePword, pword, key)) {
        Tcl_SetResult(interp, (char *)"ERROR: Failed to decrypt password.", TCL_VOLATILE);
        return TCL_ERROR;
    }

    Tcl_SetResult(interp, (char *)pword.c_str(), TCL_VOLATILE);
    free(ePword);
    free(key);
    return TCL_OK;
}
