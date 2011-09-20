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

#pragma once
#ifndef WIN32
#include <sys/types.h>
#endif
#include <string>

using namespace std;

struct SBlock
{
	//Constructors
	SBlock(unsigned int l=0, unsigned int r=0) : m_uil(l), m_uir(r) {}
	//Copy Constructor
	SBlock(const SBlock& roBlock) : m_uil(roBlock.m_uil), m_uir(roBlock.m_uir) {}
	SBlock& operator^=(SBlock& b) { m_uil ^= b.m_uil; m_uir ^= b.m_uir; return *this; }
	unsigned int m_uil, m_uir;
};

class CBlowfish
{
public:
	enum { ECB=0, CBC=1, CFB=2 };

	//Constructor - Initialize the P and S boxes for a given Key
	CBlowfish(char* ucKey, const SBlock& roChain = SBlock(0UL,0UL));

	//Resetting the chaining block
	void ResetChain() { m_oChain = m_oChain0; }

	// Encrypt/Decrypt Buffer in Place
	bool Encrypt(unsigned char* buf, size_t n, int iMode=ECB);
	bool Decrypt(unsigned char* buf, size_t n, int iMode=ECB);

	// Encrypt/Decrypt from Input Buffer to Output Buffer
	bool Encrypt(const unsigned char* in, unsigned char* out, size_t n, int iMode=ECB);
	bool Decrypt(const unsigned char* in, unsigned char* out, size_t n, int iMode=ECB);

private:
    //char *Char2Hex(const unsigned char ch, char* szHex);
	unsigned char Hex2Char(const char* szHex);
	
//Private Functions
private:
	unsigned int F(unsigned int ui);
	void Encrypt(SBlock&);
	void Decrypt(SBlock&);

private:
	//The Initialization Vector, by default {0, 0}
	SBlock m_oChain0;
	SBlock m_oChain;
	unsigned int m_auiP[18];
	unsigned int m_auiS[4][256];
	static const unsigned int scm_auiInitP[18];
	static const unsigned int scm_auiInitS[4][256];
};

//Extract low order byte
inline unsigned char Byte(unsigned int ui)
{
	return (unsigned char)(ui & 0xff);
}

//Function F
inline unsigned int CBlowfish::F(unsigned int ui)
{
	return ((m_auiS[0][Byte(ui>>24)] + m_auiS[1][Byte(ui>>16)]) ^ m_auiS[2][Byte(ui>>8)]) + m_auiS[3][Byte(ui)];
}

//NL Apr `04. (Copied from original test program.)
//Function to convert unsigned char to string of length 2
/*inline char *CBlowfish::Char2Hex(const unsigned char ch, char* szHex)
{
	unsigned char byte[2];
	byte[0] = ch/16;
	byte[1] = ch%16;
	for(int i=0; i<2; i++)
	{
		if(byte[i] >= 0 && byte[i] <= 9)
			szHex[i] = '0' + byte[i];
		else
			szHex[i] = 'A' + byte[i] - 10;
	}
	szHex[2] = 0;
	return szHex;
}*/
//Function to convert string of length 2 to unsigned char
inline unsigned char CBlowfish::Hex2Char(const char* szHex)
{
	char rch = 0;
	for(int i=0; i<2; i++)
	{
		if(*(szHex + i) >='0' && *(szHex + i) <= '9')
			rch = (rch << 4) + (*(szHex + i) - '0');
		else if(*(szHex + i) >='A' && *(szHex + i) <= 'F')
			rch = (rch << 4) + (*(szHex + i) - 'A' + 10);
		else
			break;
	}
	return rch;
}    


long encryptFile(const char *fname);
long decryptFile(const char *fname, char **buffer);
long encryptPassword(string orgPassword, string &ePassword, string key = "");
long decryptPassword(string ePassword, string &orgPassword, string key = "");
long encryptBuffer(const char *buffer, long length, char **retBuffer, const char *key = "");
long decryptBuffer(const char *buffer, long length, char **retbuffer, const char *key = "");
