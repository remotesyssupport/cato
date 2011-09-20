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

#include <string>
#include <iostream>

using namespace std;
// Note: this function allocates the output buffer so it MUST be freed by the caller
// Note: The returned buffer is NOT null terminated.  The length of the returned buffer 
// is returned by this function to tell the caller how much data was decoded
//long decodeBase64(const char *base64String, char **buffer);
//char *encodeBase64(char *buffer, long length);
string encodeBase64(unsigned char const *bytes_to_encode, unsigned int in_len);
string decodeBase64(string const& encoded_string);
