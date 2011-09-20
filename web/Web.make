

# Warning: This is an automatically generated file, do not edit!

srcdir=.
top_srcdir=.

include $(top_srcdir)/config.make

ifeq ($(CONFIG),DEBUG__NET)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG;TRACE"
ASSEMBLY = bin/Web.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin

WEB_DLL_MDB_SOURCE=bin/Web.dll.mdb
WEB_DLL_MDB=$(BUILD_DIR)/bin/Web.dll.mdb

endif

ifeq ($(CONFIG),DEBUG)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG;TRACE"
ASSEMBLY = bin/Web.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin

WEB_DLL_MDB_SOURCE=bin/Web.dll.mdb
WEB_DLL_MDB=$(BUILD_DIR)/bin/Web.dll.mdb

endif

ifeq ($(CONFIG),DEBUG_MIXED_PLATFORMS)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG;TRACE"
ASSEMBLY = bin/Web.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin

WEB_DLL_MDB_SOURCE=bin/Web.dll.mdb
WEB_DLL_MDB=$(BUILD_DIR)/bin/Web.dll.mdb

endif

ifeq ($(CONFIG),DEBUG_X86)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG;TRACE"
ASSEMBLY = bin/Web.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin

WEB_DLL_MDB_SOURCE=bin/Web.dll.mdb
WEB_DLL_MDB=$(BUILD_DIR)/bin/Web.dll.mdb

endif

ifeq ($(CONFIG),RELEASE__NET)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:TRACE"
ASSEMBLY = bin/Web.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin

WEB_DLL_MDB=

endif

ifeq ($(CONFIG),RELEASE)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:TRACE"
ASSEMBLY = bin/Web.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin

WEB_DLL_MDB=

endif

ifeq ($(CONFIG),RELEASE_MIXED_PLATFORMS)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:TRACE"
ASSEMBLY = bin/Web.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin

WEB_DLL_MDB=

endif

ifeq ($(CONFIG),RELEASE_X86)
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:TRACE"
ASSEMBLY = bin/Web.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin

WEB_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES_BIN = \
	$(WEB_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(WEB_PC)  


RESGEN=resgen2

WEB_PC = $(BUILD_DIR)/web.pc

FILES = \
	Default.aspx.cs \
	Default.aspx.designer.cs \
	login.aspx.cs \
	login.aspx.designer.cs \
	pages/about.aspx.cs \
	pages/about.aspx.designer.cs \
	pages/awsDiscovery.aspx.cs \
	pages/awsDiscovery.aspx.designer.cs \
	pages/awsMethods.asmx.cs \
	pages/cloudAccountEdit.aspx.cs \
	pages/cloudAccountEdit.aspx.designer.cs \
	pages/cloudAPITester.aspx.cs \
	pages/cloudAPITester.aspx.designer.cs \
	pages/ecoTemplateEdit.aspx.cs \
	pages/ecoTemplateEdit.aspx.designer.cs \
	pages/ecoTemplateManage.aspx.cs \
	pages/ecoTemplateManage.aspx.designer.cs \
	pages/ecosystemEdit.aspx.cs \
	pages/ecosystemEdit.aspx.designer.cs \
	pages/ecosystemManage.aspx.cs \
	pages/ecosystemManage.aspx.designer.cs \
	pages/globalRegistry.aspx.cs \
	pages/globalRegistry.aspx.designer.cs \
	pages/schedulerSettings.aspx.cs \
	pages/schedulerSettings.aspx.designer.cs \
	pages/tagEdit.aspx.cs \
	pages/tagEdit.aspx.designer.cs \
	pages/notAllowed.aspx.cs \
	pages/notAllowed.aspx.designer.cs \
	pages/taskRunLog.aspx.cs \
	pages/taskRunLog.aspx.designer.cs \
	pages/uiMethods.asmx.cs \
	pages/pollerSettings.aspx.cs \
	pages/pollerSettings.aspx.designer.cs \
	pages/taskEdit.aspx.designer.cs \
	pages/securityLogView.aspx.cs \
	pages/securityLogView.aspx.designer.cs \
	pages/taskCommandHelp.aspx.cs \
	pages/taskCommandHelp.aspx.designer.cs \
	pages/taskPrint.aspx.cs \
	pages/taskPrint.aspx.designer.cs \
	pages/taskView.aspx.cs \
	pages/ldapEdit.aspx.cs \
	pages/ldapEdit.aspx.designer.cs \
	pages/taskStatusView.aspx.cs \
	pages/taskStatusView.aspx.designer.cs \
	pages/sharedCredentialEdit.aspx.cs \
	pages/sharedCredentialEdit.aspx.designer.cs \
	pages/taskMethods.asmx.cs \
	pages/taskView.aspx.designer.cs \
	pages/userPreferenceEdit.aspx.cs \
	pages/userPreferenceEdit.aspx.designer.cs \
	pages/taskActivityLog.aspx.cs \
	pages/taskActivityLog.aspx.designer.cs \
	pages/systemStatusView.aspx.cs \
	pages/systemStatusView.aspx.designer.cs \
	pages/notificationEdit.aspx.cs \
	pages/notificationEdit.aspx.designer.cs \
	pages/taskManage.aspx.cs \
	pages/taskManage.aspx.designer.cs \
	pages/home.aspx.cs \
	pages/home.aspx.designer.cs \
	pages/loginDefaultsEdit.aspx.cs \
	pages/loginDefaultsEdit.aspx.designer.cs \
	pages/taskEdit.aspx.cs \
	pages/taskStepVarsEdit.aspx.cs \
	pages/taskStepVarsEdit.aspx.designer.cs \
	pages/userEdit.aspx.cs \
	pages/userEdit.aspx.designer.cs \
	pages/logserverSettings.aspx.cs \
	pages/logserverSettings.aspx.designer.cs \
	classes/AppGlobals.cs \
	classes/UI.cs \
	classes/dataAccess.cs \
	classes/Globals.cs \
	classes/Blowfish.cs \
	classes/CustomTemplates.cs \
	classes/HTMLTemplates.cs \
	classes/AssemblyInfo.cs \
	pages/site.master.cs \
	pages/site.master.designer.cs \
	pages/popupwindow.master.cs \
	pages/popupwindow.master.designer.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	pages/taskStepVarsEdit.aspx \
	script/taskedit/taskStepVarsEdit.js \
	script/cloudAccountEdit.js \
	script/taskRunLog.js \
	script/tagEdit.js \
	script/ldapEdit.js \
	script/taskStatusView.js \
	script/sharedCredentialEdit.js \
	script/taskActivityLog.js \
	script/systemStatusView.js \
	conf/cato.conf \
	pages/taskCommandHelp.aspx \
	conf \
	web.pc.in 

REFERENCES =  \
	System \
	System.Data \
	System.Core \
	System.Data.DataSetExtensions \
	System.DirectoryServices \
	System.Web.Extensions \
	System.Xml.Linq \
	System.Drawing \
	System.Web \
	System.Xml \
	System.Configuration \
	System.Web.Services \
	System.EnterpriseServices

DLL_REFERENCES =  \
	bin/ext/mysql.data.dll

CLEANFILES = $(PROGRAMFILES_BIN) $(LINUX_PKGCONFIG) 

#Targets
all-local: $(ASSEMBLY) $(PROGRAMFILES_BIN) $(LINUX_PKGCONFIG)  $(top_srcdir)/config.make



$(eval $(call emit-deploy-target,WEB_DLL_MDB))
$(eval $(call emit-deploy-wrapper,WEB_PC,web.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'


$(ASSEMBLY_MDB): $(ASSEMBLY)
$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	make pre-all-local-hook prefix=$(prefix)
	mkdir -p $(shell dirname $(ASSEMBLY))
	make $(CONFIG)_BeforeBuild
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
	make $(CONFIG)_AfterBuild
	make post-all-local-hook prefix=$(prefix)

install-local: $(ASSEMBLY) $(ASSEMBLY_MDB)
	make pre-install-local-hook prefix=$(prefix)
	make install-satellite-assemblies prefix=$(prefix)
	mkdir -p '$(DESTDIR)$(libdir)/$(PACKAGE)'
	$(call cp,$(ASSEMBLY),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call cp,$(ASSEMBLY_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	mkdir -p '$(DESTDIR)$(libdir)/$(PACKAGE)/bin'
	$(call cp,$(WEB_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE)/bin)
	mkdir -p '$(DESTDIR)$(libdir)/pkgconfig'
	$(call cp,$(WEB_PC),$(DESTDIR)$(libdir)/pkgconfig)
	make post-install-local-hook prefix=$(prefix)

uninstall-local: $(ASSEMBLY) $(ASSEMBLY_MDB)
	make pre-uninstall-local-hook prefix=$(prefix)
	make uninstall-satellite-assemblies prefix=$(prefix)
	$(call rm,$(ASSEMBLY),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(ASSEMBLY_MDB),$(DESTDIR)$(libdir)/$(PACKAGE))
	$(call rm,$(WEB_DLL_MDB),$(DESTDIR)$(libdir)/$(PACKAGE)/bin)
	$(call rm,$(WEB_PC),$(DESTDIR)$(libdir)/pkgconfig)
	make post-uninstall-local-hook prefix=$(prefix)
