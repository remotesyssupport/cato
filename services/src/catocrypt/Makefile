########################################
#  CONFIGURATION VARIABLES
#
# Change the following lines to match
# tell the compiler where the tcl
# files are and which version
#
########################################
TCL_INCLUDE_HOME=/usr/include/tcl8.5
TCL_LIB_HOME=/usr/lib
TCL_LIB_VERSION=8.5

########################################
#  You should not need to edit below
#  this line
########################################
GCC = `which g++`
LIBSRC = tcl_hooks.cc password.cc blowfish.cc base64.cc
LIB_SO = ../../lib/catocrypt/libcatocrypt.so
PROJ_NAME = "Cato Encryption Library"

########################################
#  VARIABLE POST-PROCESSING                                                 
########################################
LIBSRC_C = $(LIBSRC:%.cc=%.cc)
LIBSRC_O = $(LIBSRC:%.cc=%.o)

########################################
#  LIB BUILD                               
########################################
libcatocrypt: CFLAGS=-g -I$(TCL_INCLUDE_HOME) -fPIC
libcatocrypt: LDFLAGS= -L$(TCL_LIB_HOME) -ltcl$(TCL_LIB_VERSION)
libcatocrypt: PHASE="Cato Crypt Library"
libcatocrypt: sep $(LIBSRC_O)
	@echo $(GCC) $(LDFLAGS) -shared -o $(LIB_SO) $(LIBSRC_O)
	@$(GCC) $(LDFLAGS) -shared -o $(LIB_SO) $(LIBSRC_O)
	
########################################
#  SEPARATOR                                                                
########################################
sep:
	@echo -------------------------------------------------------
	@echo Building $(PROJ_NAME) \( $(PHASE) \)

########################################
#  DESTROY BINARIES                                               
########################################

clean:
	@echo "Removing binaries..."
	@echo rm -f $(LIBSRC_O) $(LIB_SO)
	@rm -f $(LIBSRC_O) $(LIB_SO)

########################################
#  BINARY DEPENDANCIES                                             
########################################
%.o: %.cc
	@printf "[33m :: compiling %-20s[0m | [36m%s[0m\n" $(<F) $(PHASE)
	@echo $(GCC) $(CFLAGS) -c $< -o $@
	@$(GCC) $(CFLAGS) -c $< -o $@
