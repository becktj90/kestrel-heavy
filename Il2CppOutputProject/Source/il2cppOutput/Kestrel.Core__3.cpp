#include "pch-cpp.hpp"





template <typename T1, typename T2>
struct InterfaceActionInvoker2
{
	typedef void (*Action)(void*, T1, T2, const RuntimeMethod*);

	static inline void Invoke (Il2CppMethodSlot slot, RuntimeClass* declaringInterface, RuntimeObject* obj, T1 p1, T2 p2)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_interface_invoke_data(slot, obj, declaringInterface);
		((Action)invokeData.methodPtr)(obj, p1, p2, invokeData.method);
	}
};
template <typename T1, typename T2, typename T3>
struct InterfaceActionInvoker3
{
	typedef void (*Action)(void*, T1, T2, T3, const RuntimeMethod*);

	static inline void Invoke (Il2CppMethodSlot slot, RuntimeClass* declaringInterface, RuntimeObject* obj, T1 p1, T2 p2, T3 p3)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_interface_invoke_data(slot, obj, declaringInterface);
		((Action)invokeData.methodPtr)(obj, p1, p2, p3, invokeData.method);
	}
};
template <typename T1, typename T2, typename T3, typename T4, typename T5, typename T6>
struct InterfaceActionInvoker6
{
	typedef void (*Action)(void*, T1, T2, T3, T4, T5, T6, const RuntimeMethod*);

	static inline void Invoke (Il2CppMethodSlot slot, RuntimeClass* declaringInterface, RuntimeObject* obj, T1 p1, T2 p2, T3 p3, T4 p4, T5 p5, T6 p6)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_interface_invoke_data(slot, obj, declaringInterface);
		((Action)invokeData.methodPtr)(obj, p1, p2, p3, p4, p5, p6, invokeData.method);
	}
};
template <typename R>
struct InterfaceFuncInvoker0
{
	typedef R (*Func)(void*, const RuntimeMethod*);

	static inline R Invoke (Il2CppMethodSlot slot, RuntimeClass* declaringInterface, RuntimeObject* obj)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_interface_invoke_data(slot, obj, declaringInterface);
		return ((Func)invokeData.methodPtr)(obj, invokeData.method);
	}
};
template <typename R, typename T1>
struct InterfaceFuncInvoker1
{
	typedef R (*Func)(void*, T1, const RuntimeMethod*);

	static inline R Invoke (Il2CppMethodSlot slot, RuntimeClass* declaringInterface, RuntimeObject* obj, T1 p1)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_interface_invoke_data(slot, obj, declaringInterface);
		return ((Func)invokeData.methodPtr)(obj, p1, invokeData.method);
	}
};

struct Action_1_tD69A6DC9FBE94131E52F5A73B2A9D4AB51EEC404;
struct Dictionary_2_t9FA6D82CAFC18769F7515BB51D1C56DAE09381C3;
struct Dictionary_2_tE1603CE612C16451D1E56FF4D4859D4FE4087C28;
struct IList_1_t90735131CC8058817BB0091BD9AB0D05090C0A58;
struct List_1_tAB32C91F17A9EF1C977FD736177219D0D9DD679A;
struct List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D;
struct List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5;
struct List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8;
struct List_1_tF470A3BE5C1B5B68E1325EF3F109D172E60BD7CD;
struct ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031;
struct CharU5BU5D_t799905CF001DD5F13F7DBB310181FC4D8B7D0AAB;
struct DelegateU5BU5D_tC5AB7E8F745616680F337909D3A8E6C722CDF771;
struct DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE;
struct FlightRiskU5BU5D_t78F88C5B752B50C229A77BF30F51E2426C3209F6;
struct IntPtrU5BU5D_tFD177F8C806A6921AD7150264CCC62FA00CAD832;
struct ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918;
struct SimEventU5BU5D_t76B06325B23B376D30933AE8A8FB4C34665D72A6;
struct SimSampleU5BU5D_t5841B807530E6C141503B88A570CA4B3CA13A10B;
struct SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641;
struct StackTraceU5BU5D_t32FBCB20930EAF5BAE3F450FF75228E5450DA0DF;
struct StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248;
struct InfoU5BU5D_t9EB0B7CDB931F37ACDAC8EBD9C98135AF4EB61D1;
struct KnotU5BU5D_tB71A6CF8875FFDD1F88C8F6D905D1F244D835321;
struct ArgumentException_tAD90411542A20A9C72D5CDA3A84181D8B947A263;
struct ArgumentNullException_t327031E412FAB2351B0022DD5DAD47E67E597129;
struct AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C;
struct AsyncCallback_t7FEF460CBDCFB9C5FA2EF776984778B9A4145F4C;
struct Calendar_t0A117CC7532A54C17188C2EFEA1F79DB20DF3A3B;
struct CompareInfo_t1B1A6AC3486B570C76ABA52149C9BD4CD82F9E57;
struct CultureData_tEEFDCF4ECA1BBF6C0C8C94EB3541657245598F9D;
struct CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0;
struct DateTimeFormatInfo_t0457520F9FA7B5C8EAAEB3AD50413B6AEEB7458A;
struct DelegateData_t9B286B493293CD2D23A5B2B5EF0E5B1324C2B77E;
struct Deriv_t32B789F19EA5F7301FEE48A702C2B32AF64EF69A;
struct EventSchedule_t09CF461F53E916CBA1AB9742A81E250D1F273E2D;
struct FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57;
struct FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146;
struct GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307;
struct IAsyncResult_t7B9B5A0ECB35DCEC31B8A8122C37D687369253B5;
struct IDictionary_t6D03155AF1FA9083817AA5B6AD7DEEACC26AB220;
struct IFlightSimSink_t6B2E44FEAA2B83A736D6ADA46993E42135AF5A95;
struct IFormatProvider_tC202922D43BFF3525109ABF3FB79625F5646AB52;
struct MethodInfo_t;
struct NumberFormatInfo_t8E26808B202927FEBF9064FCFEEA4D6E076E6472;
struct RK4_t1BC6735398DED08AF0675CEEB7983FDA7AB140CB;
struct SafeSerializationManager_tCBB85B95DFD1634237140CD892E82D06ECB3F5E6;
struct SeededWind_t078A40A35FF4627B5FA2AA1AE748CF01648BD194;
struct SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9;
struct SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B;
struct SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD;
struct SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E;
struct String_t;
struct TextInfo_tD3BAFCFD77418851E7D5CB8D2588F47019E414B4;
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915;
struct U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973;

IL2CPP_EXTERN_C RuntimeClass* Action_1_tD69A6DC9FBE94131E52F5A73B2A9D4AB51EEC404_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* ArgumentException_tAD90411542A20A9C72D5CDA3A84181D8B947A263_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* ArgumentNullException_t327031E412FAB2351B0022DD5DAD47E67E597129_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Deriv_t32B789F19EA5F7301FEE48A702C2B32AF64EF69A_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* ICollection_1_t88F05726DAA7C0AE3C0938257EA5DEF9546C2B94_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IFlightSimSink_t6B2E44FEAA2B83A736D6ADA46993E42135AF5A95_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IList_1_t90735131CC8058817BB0091BD9AB0D05090C0A58_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* List_1_tF470A3BE5C1B5B68E1325EF3F109D172E60BD7CD_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Parallel_tEDD920942C8F814D55C6B029EACB00C81620BD14_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* RK4_t1BC6735398DED08AF0675CEEB7983FDA7AB140CB_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C String_t* _stringLiteral0801BF8830970233C3B134BEF1CE110C18B5CCC7;
IL2CPP_EXTERN_C String_t* _stringLiteral0807218A785BE305BE2414A2A176495BD6EFBF7E;
IL2CPP_EXTERN_C String_t* _stringLiteral1DD92AA4AFE85FDFA27394CEC51EF50B82AFF2A0;
IL2CPP_EXTERN_C String_t* _stringLiteral1E3DB61522C4E7AA1D5A216FFA2CA92BAFA2EBF1;
IL2CPP_EXTERN_C String_t* _stringLiteral1F60DDB24B17EAD7ECD8D55CDEA85F2C9C741B97;
IL2CPP_EXTERN_C String_t* _stringLiteral4044F71E1BE65840634E2AF1C09536F10DBF2AF5;
IL2CPP_EXTERN_C String_t* _stringLiteral48DF3B432B8D62D5F8D80DE53D4D9FCC446CD284;
IL2CPP_EXTERN_C String_t* _stringLiteral5040608BDCF01CCCC47DE48F5A4067A404B37F88;
IL2CPP_EXTERN_C String_t* _stringLiteral5574CCCEF712B5D9AF4D4A9BC634BC330C41F6EE;
IL2CPP_EXTERN_C String_t* _stringLiteral852EFDF643FA620806D7A0FC961E635054FFCC2B;
IL2CPP_EXTERN_C String_t* _stringLiteral88CE3FCD887A58F8A5764032FA39B0C1CD48440B;
IL2CPP_EXTERN_C String_t* _stringLiteralA2B26C8EC14B2C05545E961B44B92631AA65EBE5;
IL2CPP_EXTERN_C String_t* _stringLiteralAD88ECBD135D7E7861EE52F538BEC1F9767085EA;
IL2CPP_EXTERN_C String_t* _stringLiteralADFC4849E68D1978E71A9D4530D530AB1F600C39;
IL2CPP_EXTERN_C String_t* _stringLiteralC22AD2B06041300DA8ABAAA301A2E02B285863B3;
IL2CPP_EXTERN_C String_t* _stringLiteralCDE83734553D6AC0E1530734343508D1FC27A13F;
IL2CPP_EXTERN_C String_t* _stringLiteralD991FE6C9AC0B4EDFA368D323F13614F48EBFCB3;
IL2CPP_EXTERN_C String_t* _stringLiteralDA39A3EE5E6B4B0D3255BFEF95601890AFD80709;
IL2CPP_EXTERN_C const RuntimeMethod* AscentSim_Deriv_m4147891C17132CF460ABF67F65FA78578EE2235A_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Atmosphere_SampleDensity_mEE9E9E2AAB60F1889CB2AEC36D9B3CF43B7083ED_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1_Add_m67EF1200FCD49355EECE77EB754C2EED52F8B3DE_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1_Add_mF10DB1D3CBB0B14215F0E4F8AB4934A1955E5351_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1__ctor_m17F0A7DA25FD5C6456AA73F6E13F0869DB851C82_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1__ctor_mC776F2E1B0BF7A914DD5F7B2DB3E7B333D09A513_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1__ctor_mCA8DD57EAC70C2B5923DBB9D5A77CEAC22E7068E_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Nullable_1_GetValueOrDefault_mC5602D6D01DDB3D6B4DD87E77F20FA65CFFD00C4_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Nullable_1_ToString_m5FDC0CBE068DB2893454257CE6E29846D47B3038_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Nullable_1__ctor_m141FA88563AC0B5179132FB929EABD02C47FF703_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Nullable_1_get_HasValue_m5292804A8580CD2DD32F958EC5B17A9025B3004A_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Nullable_1_get_HasValue_mCF2FD8B3055FA87FC9C504F2122B3B0FAEDE3EC9_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* U3CU3Ec__DisplayClass13_0_U3CSampleDensityU3Eb__0_m25010DB8E12C39982D936438B2B5A9E9A4635998_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* ValueTuple_2__ctor_m945C088412D024EDD72CDA156A7A63BB5E0D0688_RuntimeMethod_var;
struct CultureData_tEEFDCF4ECA1BBF6C0C8C94EB3541657245598F9D_marshaled_com;
struct CultureData_tEEFDCF4ECA1BBF6C0C8C94EB3541657245598F9D_marshaled_pinvoke;
struct CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0_marshaled_com;
struct CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0_marshaled_pinvoke;
struct Delegate_t_marshaled_com;
struct Delegate_t_marshaled_pinvoke;
struct Exception_t_marshaled_com;
struct Exception_t_marshaled_pinvoke;

struct DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE;
struct ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918;
struct SimSampleU5BU5D_t5841B807530E6C141503B88A570CA4B3CA13A10B;
struct SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641;

IL2CPP_EXTERN_C_BEGIN
IL2CPP_EXTERN_C_END

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
struct List_1_tAB32C91F17A9EF1C977FD736177219D0D9DD679A  : public RuntimeObject
{
	FlightRiskU5BU5D_t78F88C5B752B50C229A77BF30F51E2426C3209F6* ____items;
	int32_t ____size;
	int32_t ____version;
	RuntimeObject* ____syncRoot;
};
struct List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D  : public RuntimeObject
{
	ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918* ____items;
	int32_t ____size;
	int32_t ____version;
	RuntimeObject* ____syncRoot;
};
struct List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5  : public RuntimeObject
{
	SimEventU5BU5D_t76B06325B23B376D30933AE8A8FB4C34665D72A6* ____items;
	int32_t ____size;
	int32_t ____version;
	RuntimeObject* ____syncRoot;
};
struct List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8  : public RuntimeObject
{
	SimSampleU5BU5D_t5841B807530E6C141503B88A570CA4B3CA13A10B* ____items;
	int32_t ____size;
	int32_t ____version;
	RuntimeObject* ____syncRoot;
};
struct List_1_tF470A3BE5C1B5B68E1325EF3F109D172E60BD7CD  : public RuntimeObject
{
	StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* ____items;
	int32_t ____size;
	int32_t ____version;
	RuntimeObject* ____syncRoot;
};
struct Atmosphere_tDEE3F64803FE55AD7BCE347F94BD050B463FEA19  : public RuntimeObject
{
};
struct CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0  : public RuntimeObject
{
	bool ___m_isReadOnly;
	int32_t ___cultureID;
	int32_t ___parent_lcid;
	int32_t ___datetime_index;
	int32_t ___number_index;
	int32_t ___default_calendar_type;
	bool ___m_useUserOverride;
	NumberFormatInfo_t8E26808B202927FEBF9064FCFEEA4D6E076E6472* ___numInfo;
	DateTimeFormatInfo_t0457520F9FA7B5C8EAAEB3AD50413B6AEEB7458A* ___dateTimeInfo;
	TextInfo_tD3BAFCFD77418851E7D5CB8D2588F47019E414B4* ___textInfo;
	String_t* ___m_name;
	String_t* ___englishname;
	String_t* ___nativename;
	String_t* ___iso3lang;
	String_t* ___iso2lang;
	String_t* ___win3lang;
	String_t* ___territory;
	StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* ___native_calendar_names;
	CompareInfo_t1B1A6AC3486B570C76ABA52149C9BD4CD82F9E57* ___compareInfo;
	void* ___textinfo_data;
	int32_t ___m_dataItem;
	Calendar_t0A117CC7532A54C17188C2EFEA1F79DB20DF3A3B* ___calendar;
	CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0* ___parent_culture;
	bool ___constructed;
	ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___cached_serialized_form;
	CultureData_tEEFDCF4ECA1BBF6C0C8C94EB3541657245598F9D* ___m_cultureData;
	bool ___m_isInherited;
};
struct CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0_marshaled_pinvoke
{
	int32_t ___m_isReadOnly;
	int32_t ___cultureID;
	int32_t ___parent_lcid;
	int32_t ___datetime_index;
	int32_t ___number_index;
	int32_t ___default_calendar_type;
	int32_t ___m_useUserOverride;
	NumberFormatInfo_t8E26808B202927FEBF9064FCFEEA4D6E076E6472* ___numInfo;
	DateTimeFormatInfo_t0457520F9FA7B5C8EAAEB3AD50413B6AEEB7458A* ___dateTimeInfo;
	TextInfo_tD3BAFCFD77418851E7D5CB8D2588F47019E414B4* ___textInfo;
	char* ___m_name;
	char* ___englishname;
	char* ___nativename;
	char* ___iso3lang;
	char* ___iso2lang;
	char* ___win3lang;
	char* ___territory;
	char** ___native_calendar_names;
	CompareInfo_t1B1A6AC3486B570C76ABA52149C9BD4CD82F9E57* ___compareInfo;
	void* ___textinfo_data;
	int32_t ___m_dataItem;
	Calendar_t0A117CC7532A54C17188C2EFEA1F79DB20DF3A3B* ___calendar;
	CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0_marshaled_pinvoke* ___parent_culture;
	int32_t ___constructed;
	Il2CppSafeArray* ___cached_serialized_form;
	CultureData_tEEFDCF4ECA1BBF6C0C8C94EB3541657245598F9D_marshaled_pinvoke* ___m_cultureData;
	int32_t ___m_isInherited;
};
struct CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0_marshaled_com
{
	int32_t ___m_isReadOnly;
	int32_t ___cultureID;
	int32_t ___parent_lcid;
	int32_t ___datetime_index;
	int32_t ___number_index;
	int32_t ___default_calendar_type;
	int32_t ___m_useUserOverride;
	NumberFormatInfo_t8E26808B202927FEBF9064FCFEEA4D6E076E6472* ___numInfo;
	DateTimeFormatInfo_t0457520F9FA7B5C8EAAEB3AD50413B6AEEB7458A* ___dateTimeInfo;
	TextInfo_tD3BAFCFD77418851E7D5CB8D2588F47019E414B4* ___textInfo;
	Il2CppChar* ___m_name;
	Il2CppChar* ___englishname;
	Il2CppChar* ___nativename;
	Il2CppChar* ___iso3lang;
	Il2CppChar* ___iso2lang;
	Il2CppChar* ___win3lang;
	Il2CppChar* ___territory;
	Il2CppChar** ___native_calendar_names;
	CompareInfo_t1B1A6AC3486B570C76ABA52149C9BD4CD82F9E57* ___compareInfo;
	void* ___textinfo_data;
	int32_t ___m_dataItem;
	Calendar_t0A117CC7532A54C17188C2EFEA1F79DB20DF3A3B* ___calendar;
	CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0_marshaled_com* ___parent_culture;
	int32_t ___constructed;
	Il2CppSafeArray* ___cached_serialized_form;
	CultureData_tEEFDCF4ECA1BBF6C0C8C94EB3541657245598F9D_marshaled_com* ___m_cultureData;
	int32_t ___m_isInherited;
};
struct Earth_t9F5505FA4D6E2269BE9D298F4CC98B7C6B27BEB1  : public RuntimeObject
{
};
struct FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57  : public RuntimeObject
{
	double ___t;
	String_t* ___kind;
	String_t* ___cause;
	String_t* ___fixHint;
	String_t* ___kid;
	String_t* ___engineer;
	int32_t ___stage;
	int32_t ___engineIndex;
	double ___value;
	double ___limit;
};
struct Frames_t19E24CDBBEC0735693495D07B99BB9B772BA2480  : public RuntimeObject
{
};
struct Geopotential_t173FA4A20676A0BFB0277F5A5492ACBE56D1B3BD  : public RuntimeObject
{
};
struct Gravity_t45378A20638101A1FE2531DFAB55592068D9190D  : public RuntimeObject
{
};
struct GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307  : public RuntimeObject
{
	bool ___useAltitudeKick;
	double ___pitchKickAlt;
	double ___pitchKickDeg;
	double ___kickSpeed;
	bool ___turnShapeSet;
	double ___turnShape;
	bool ___maxQThrottleSet;
	double ___maxQThrottle;
	bool ___gLimitSet;
	double ___gLimit;
	double ___targetAlt;
};
struct Handoff_t156FD65E0FC3563B2C48AE44611D6255CCC0E1AB  : public RuntimeObject
{
};
struct Headless_t370A68C9F0AD7AD71CDE6CA92CE7A859651B8AEE  : public RuntimeObject
{
};
struct RK4_t1BC6735398DED08AF0675CEEB7983FDA7AB140CB  : public RuntimeObject
{
	int32_t ___n;
	DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___k1;
	DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___k2;
	DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___k3;
	DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___k4;
	DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___tmp;
};
struct SeededWind_t078A40A35FF4627B5FA2AA1AE748CF01648BD194  : public RuntimeObject
{
	DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___AltitudeM;
	DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___EastMs;
	DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___NorthMs;
	double ___SurfaceMs;
	double ___DirectionFromDeg;
	double ___ShearPerSec;
	String_t* ___Compass;
	bool ___Calm;
};
struct SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B  : public RuntimeObject
{
	double ___payload;
	double ___targetAlt;
	double ___latitude;
	double ___dt;
	double ___tMax;
	double ___kickSpeed;
	double ___kickAngleDeg;
	double ___sampleEvery;
	bool ___vacuum;
	bool ___rotate;
	bool ___cutoff;
	GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307* ___guidance;
	FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* ___monitor;
	SeededWind_t078A40A35FF4627B5FA2AA1AE748CF01648BD194* ___wind;
	double ___fairingJettisonAltM;
};
struct SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E  : public RuntimeObject
{
	double ___thrustSL;
	double ___thrustVac;
	double ___ispSL;
	double ___ispVac;
	double ___propMass;
	double ___dryMass;
	double ___diameter;
	double ___cd;
	double ___throttleMin;
	double ___mdot;
	double ___area;
	String_t* ___warn;
};
struct String_t  : public RuntimeObject
{
	int32_t ____stringLength;
	Il2CppChar ____firstChar;
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F  : public RuntimeObject
{
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_pinvoke
{
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_com
{
};
struct U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973  : public RuntimeObject
{
	DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___rho;
	DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___altitudeM;
};
struct Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 
{
	bool ___hasValue;
	double ___value;
};
struct Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28 
{
	bool ___hasValue;
	int32_t ___value;
};
struct Nullable_1_t365991B3904FDA7642A788423B28692FDC7CDB17 
{
	bool ___hasValue;
	int64_t ___value;
};
struct ValueTuple_2_tCD83B33C76656F32DCB440F5228F21BF25762DE2 
{
	DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___Item1;
	double ___Item2;
};
struct ValueTuple_2_t6535F6090E6D3C9A7A645F4BFD8ED400CBA8B14C 
{
	double ___Item1;
	double ___Item2;
};
struct ValueTuple_2_t6CFD401E32E7EA9EA13C8BA9C5CA4C6BA8F73E5C 
{
	RuntimeObject* ___Item1;
	double ___Item2;
};
struct AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 
{
	double ___T;
	double ___p;
	double ___rho;
	double ___a;
};
struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22 
{
	bool ___m_value;
};
struct Byte_t94D9231AC217BE4D2E004C4CD32DF6D099EA41A3 
{
	uint8_t ___m_value;
};
struct ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 
{
	double ___a;
	double ___e;
	double ___i;
	double ___raan;
	double ___argp;
	double ___m;
};
struct Double_tE150EF3D1D43DEE85D533810AB4C742307EEDE5F 
{
	double ___m_value;
};
struct Enum_t2A1A94B24E3B776EEF4E5E485E290BB9D4D072E2  : public ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F
{
};
struct Enum_t2A1A94B24E3B776EEF4E5E485E290BB9D4D072E2_marshaled_pinvoke
{
};
struct Enum_t2A1A94B24E3B776EEF4E5E485E290BB9D4D072E2_marshaled_com
{
};
struct FailureScratch_t0A1C23C1474067DE1795979BAA2A6A3262AD5CE6 
{
	bool ___tumbleOn;
	double ___tumbleT0;
	uint64_t ___rng;
	bool ___closed;
	int32_t ___rolledStage;
	double ___thrustFactor;
	int32_t ___enginesOut;
	double ___qPeak;
	double ___gPeak;
	double ___minStability;
	double ___minCar;
};
struct FailureScratch_t0A1C23C1474067DE1795979BAA2A6A3262AD5CE6_marshaled_pinvoke
{
	int32_t ___tumbleOn;
	double ___tumbleT0;
	uint64_t ___rng;
	int32_t ___closed;
	int32_t ___rolledStage;
	double ___thrustFactor;
	int32_t ___enginesOut;
	double ___qPeak;
	double ___gPeak;
	double ___minStability;
	double ___minCar;
};
struct FailureScratch_t0A1C23C1474067DE1795979BAA2A6A3262AD5CE6_marshaled_com
{
	int32_t ___tumbleOn;
	double ___tumbleT0;
	uint64_t ___rng;
	int32_t ___closed;
	int32_t ___rolledStage;
	double ___thrustFactor;
	int32_t ___enginesOut;
	double ___qPeak;
	double ___gPeak;
	double ___minStability;
	double ___minCar;
};
struct FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 
{
	double ___X;
	double ___Y;
};
struct GuidanceState_tA40A6AE2010623D64FC40402D0D31113286A0401 
{
	double ___alt;
	double ___vrel;
	double ___flightPathRad;
	double ___vr;
	double ___g;
	double ___q;
	double ___mass;
	double ___thrustN;
	double ___throttleMin;
	int32_t ___stageIndex;
};
struct Int32_t680FF22E76F6EFAD4375103CBBFFA0421349384C 
{
	int32_t ___m_value;
};
struct Int64_t092CFB123BE63C28ACDAF65C68F21A526050DBA3 
{
	int64_t ___m_value;
};
struct IntPtr_t 
{
	void* ___m_value;
};
struct MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 
{
	double ___a;
	double ___e;
	double ___i;
	double ___raan;
	double ___argp;
	double ___m;
};
struct OrbitElements_t5EEC91C4BE0D34654FC26DBE704E101E14BB7D98 
{
	double ___a;
	double ___e;
	double ___i;
	double ___energy;
	double ___h;
	double ___periapsis;
	double ___apoapsis;
	double ___periAlt;
	double ___apoAlt;
	double ___period;
};
struct SecularRates_tD523392B109E8AC95C21E767F08383D312C0AB73 
{
	double ___n;
	double ___dOmega;
	double ___dArgp;
	double ___dMeanAnom;
};
struct SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121 
{
	double ___t;
	double ___alt;
	double ___v;
	double ___vrel;
	double ___q;
	double ___m;
	double ___pitch;
	int32_t ___stage;
	int32_t ___mode;
};
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915 
{
	union
	{
		struct
		{
		};
		uint8_t Void_t4861ACF8F4594C3437BB48B6E56783494B843915__padding[1];
	};
};
struct Step_t7AA160BDF44184A07EA62B87484E56D55AC7C795 
{
	double ___t;
	double ___dt;
	double ___alt;
	double ___qPa;
	double ___accel;
	double ___alphaRad;
	double ___mass;
	double ___area;
	double ___cd;
	double ___vDown;
	int32_t ___stageIndex;
	double ___propFraction;
	bool ___thrusting;
	bool ___emptyLast;
	double ___targetAlt;
};
struct Step_t7AA160BDF44184A07EA62B87484E56D55AC7C795_marshaled_pinvoke
{
	double ___t;
	double ___dt;
	double ___alt;
	double ___qPa;
	double ___accel;
	double ___alphaRad;
	double ___mass;
	double ___area;
	double ___cd;
	double ___vDown;
	int32_t ___stageIndex;
	double ___propFraction;
	int32_t ___thrusting;
	int32_t ___emptyLast;
	double ___targetAlt;
};
struct Step_t7AA160BDF44184A07EA62B87484E56D55AC7C795_marshaled_com
{
	double ___t;
	double ___dt;
	double ___alt;
	double ___qPa;
	double ___accel;
	double ___alphaRad;
	double ___mass;
	double ___area;
	double ___cd;
	double ___vDown;
	int32_t ___stageIndex;
	double ___propFraction;
	int32_t ___thrusting;
	int32_t ___emptyLast;
	double ___targetAlt;
};
struct ByReference_1_t4505442DB99F393450BB394E5A719E096875B1AC 
{
	intptr_t ____value;
};
struct AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C  : public RuntimeObject
{
	RuntimeObject* ___stagesIn;
	SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* ___st;
	SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* ___o;
	DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___upperMass;
	double ___vSite;
	double ___omegaAir;
	double ___kickAngle;
	RK4_t1BC6735398DED08AF0675CEEB7983FDA7AB140CB* ___rk;
	Deriv_t32B789F19EA5F7301FEE48A702C2B32AF64EF69A* ___deriv;
	DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___Y;
	int32_t ___U3CStageIdxU3Ek__BackingField;
	int32_t ___U3CModeU3Ek__BackingField;
	double ___U3CPitchCmdU3Ek__BackingField;
	bool ___U3CThrustingU3Ek__BackingField;
	double ___U3CTU3Ek__BackingField;
	bool ___U3CDoneU3Ek__BackingField;
	List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5* ___Events;
	List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8* ___Samples;
	double ___maxQ;
	double ___maxQTime;
	double ___maxQAlt;
	double ___maxG;
	double ___lossG;
	double ___lossD;
	double ___lossS;
	double ___dvExp;
	double ___nextSample;
	double ___kickStart;
	double ___thrustScale;
	double ___catchPitch;
	bool ___turned;
	bool ___liftedOff;
	bool ___sawKarman;
	bool ___maxQPassed;
	bool ___fairingSent;
	bool ___ignitionHooked;
	double ___karmanT;
	String_t* ___outcome;
	FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* ___playedBreak;
	FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 ___pad;
	double ___LastP;
	double ___LastQ;
	double ___LastMach;
	double ___LastThrust;
	double ___LiveThrottleScale;
	double ___LivePitchOffsetRad;
	RuntimeObject* ___U3CSinkU3Ek__BackingField;
};
struct CoastMode_t08B0DDF47FE66372EF32BE16D4A4A86B79659188 
{
	int32_t ___value__;
};
struct Delegate_t  : public RuntimeObject
{
	intptr_t ___method_ptr;
	intptr_t ___invoke_impl;
	RuntimeObject* ___m_target;
	intptr_t ___method;
	intptr_t ___delegate_trampoline;
	intptr_t ___extra_arg;
	intptr_t ___method_code;
	intptr_t ___interp_method;
	intptr_t ___interp_invoke_impl;
	MethodInfo_t* ___method_info;
	MethodInfo_t* ___original_method_info;
	DelegateData_t9B286B493293CD2D23A5B2B5EF0E5B1324C2B77E* ___data;
	bool ___method_is_virtual;
};
struct Delegate_t_marshaled_pinvoke
{
	intptr_t ___method_ptr;
	intptr_t ___invoke_impl;
	Il2CppIUnknown* ___m_target;
	intptr_t ___method;
	intptr_t ___delegate_trampoline;
	intptr_t ___extra_arg;
	intptr_t ___method_code;
	intptr_t ___interp_method;
	intptr_t ___interp_invoke_impl;
	MethodInfo_t* ___method_info;
	MethodInfo_t* ___original_method_info;
	DelegateData_t9B286B493293CD2D23A5B2B5EF0E5B1324C2B77E* ___data;
	int32_t ___method_is_virtual;
};
struct Delegate_t_marshaled_com
{
	intptr_t ___method_ptr;
	intptr_t ___invoke_impl;
	Il2CppIUnknown* ___m_target;
	intptr_t ___method;
	intptr_t ___delegate_trampoline;
	intptr_t ___extra_arg;
	intptr_t ___method_code;
	intptr_t ___interp_method;
	intptr_t ___interp_invoke_impl;
	MethodInfo_t* ___method_info;
	MethodInfo_t* ___original_method_info;
	DelegateData_t9B286B493293CD2D23A5B2B5EF0E5B1324C2B77E* ___data;
	int32_t ___method_is_virtual;
};
struct Exception_t  : public RuntimeObject
{
	String_t* ____className;
	String_t* ____message;
	RuntimeObject* ____data;
	Exception_t* ____innerException;
	String_t* ____helpURL;
	RuntimeObject* ____stackTrace;
	String_t* ____stackTraceString;
	String_t* ____remoteStackTraceString;
	int32_t ____remoteStackIndex;
	RuntimeObject* ____dynamicMethods;
	int32_t ____HResult;
	String_t* ____source;
	SafeSerializationManager_tCBB85B95DFD1634237140CD892E82D06ECB3F5E6* ____safeSerializationManager;
	StackTraceU5BU5D_t32FBCB20930EAF5BAE3F450FF75228E5450DA0DF* ___captured_traces;
	IntPtrU5BU5D_tFD177F8C806A6921AD7150264CCC62FA00CAD832* ___native_trace_ips;
	int32_t ___caught_in_unmanaged;
};
struct Exception_t_marshaled_pinvoke
{
	char* ____className;
	char* ____message;
	RuntimeObject* ____data;
	Exception_t_marshaled_pinvoke* ____innerException;
	char* ____helpURL;
	Il2CppIUnknown* ____stackTrace;
	char* ____stackTraceString;
	char* ____remoteStackTraceString;
	int32_t ____remoteStackIndex;
	Il2CppIUnknown* ____dynamicMethods;
	int32_t ____HResult;
	char* ____source;
	SafeSerializationManager_tCBB85B95DFD1634237140CD892E82D06ECB3F5E6* ____safeSerializationManager;
	StackTraceU5BU5D_t32FBCB20930EAF5BAE3F450FF75228E5450DA0DF* ___captured_traces;
	Il2CppSafeArray* ___native_trace_ips;
	int32_t ___caught_in_unmanaged;
};
struct Exception_t_marshaled_com
{
	Il2CppChar* ____className;
	Il2CppChar* ____message;
	RuntimeObject* ____data;
	Exception_t_marshaled_com* ____innerException;
	Il2CppChar* ____helpURL;
	Il2CppIUnknown* ____stackTrace;
	Il2CppChar* ____stackTraceString;
	Il2CppChar* ____remoteStackTraceString;
	int32_t ____remoteStackIndex;
	Il2CppIUnknown* ____dynamicMethods;
	int32_t ____HResult;
	Il2CppChar* ____source;
	SafeSerializationManager_tCBB85B95DFD1634237140CD892E82D06ECB3F5E6* ____safeSerializationManager;
	StackTraceU5BU5D_t32FBCB20930EAF5BAE3F450FF75228E5450DA0DF* ___captured_traces;
	Il2CppSafeArray* ___native_trace_ips;
	int32_t ___caught_in_unmanaged;
};
struct FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146  : public RuntimeObject
{
	InfoU5BU5D_t9EB0B7CDB931F37ACDAC8EBD9C98135AF4EB61D1* ___infos;
	KnotU5BU5D_tB71A6CF8875FFDD1F88C8F6D905D1F244D835321* ___knots;
	int32_t ___stages;
	double ___qLimPa;
	double ___limitG;
	double ___maxTempK;
	double ___heatFactor;
	double ___vLeg;
	bool ___legs;
	bool ___gentle;
	EventSchedule_t09CF461F53E916CBA1AB9742A81E250D1F273E2D* ___schedule;
	FailureScratch_t0A1C23C1474067DE1795979BAA2A6A3262AD5CE6 ___scratch;
	double ___sepV;
	double ___sepT;
	bool ___sepLive;
	int32_t ___sepStage;
	bool ___Predict;
	bool ___Gentle;
	FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* ___Event;
	List_1_tAB32C91F17A9EF1C977FD736177219D0D9DD679A* ___Risks;
};
struct Int32Enum_tCBAC8BA2BFF3A845FA599F303093BBBA374B6F0C 
{
	int32_t ___value__;
};
struct ParallelLoopResult_t9C383DA248DA9082ECBF057DE0879AB99EE6A734 
{
	bool ____completed;
	Nullable_1_t365991B3904FDA7642A788423B28692FDC7CDB17 ____lowestBreakIteration;
};
struct ParallelLoopResult_t9C383DA248DA9082ECBF057DE0879AB99EE6A734_marshaled_pinvoke
{
	int32_t ____completed;
	Nullable_1_t365991B3904FDA7642A788423B28692FDC7CDB17 ____lowestBreakIteration;
};
struct ParallelLoopResult_t9C383DA248DA9082ECBF057DE0879AB99EE6A734_marshaled_com
{
	int32_t ____completed;
	Nullable_1_t365991B3904FDA7642A788423B28692FDC7CDB17 ____lowestBreakIteration;
};
struct SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9  : public RuntimeObject
{
	double ___t;
	String_t* ___type;
	Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28 ___stage;
	Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 ___alt;
	Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 ___v;
	Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 ___apoAlt;
	Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 ___periAlt;
};
struct SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD  : public RuntimeObject
{
	String_t* ___outcome;
	double ___time;
	List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5* ___events;
	OrbitElements_t5EEC91C4BE0D34654FC26DBE704E101E14BB7D98 ___orbit;
	double ___maxQ;
	double ___maxQTime;
	double ___maxQAlt;
	double ___maxG;
	double ___lossGravity;
	double ___lossDrag;
	double ___lossSteering;
	double ___idealDeltaV;
	double ___deltaVExpended;
	double ___finalMass;
	double ___x;
	double ___y;
	double ___vx;
	double ___vy;
	double ___m;
	List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8* ___samples;
	List_1_tF470A3BE5C1B5B68E1325EF3F109D172E60BD7CD* ___warnings;
	FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* ___failure;
	List_1_tAB32C91F17A9EF1C977FD736177219D0D9DD679A* ___risks;
	double ___minStability;
	double ___minCar;
	double ___qLimPa;
	double ___coastApexAlt;
	Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 ___KarmanTime;
};
struct Nullable_1_t3EE493957A8C36BFB8CFAD3C76358D1249FDDFA0 
{
	bool ___hasValue;
	int32_t ___value;
};
struct Nullable_1_t163D49A1147F217B7BD43BE8ACC8A5CC6B846D14 
{
	bool ___hasValue;
	int32_t ___value;
};
struct Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5 
{
	ByReference_1_t4505442DB99F393450BB394E5A719E096875B1AC ____pointer;
	int32_t ____length;
};
struct MulticastDelegate_t  : public Delegate_t
{
	DelegateU5BU5D_tC5AB7E8F745616680F337909D3A8E6C722CDF771* ___delegates;
};
struct MulticastDelegate_t_marshaled_pinvoke : public Delegate_t_marshaled_pinvoke
{
	Delegate_t_marshaled_pinvoke** ___delegates;
};
struct MulticastDelegate_t_marshaled_com : public Delegate_t_marshaled_com
{
	Delegate_t_marshaled_com** ___delegates;
};
struct SystemException_tCC48D868298F4C0705279823E34B00F4FBDB7295  : public Exception_t
{
};
struct Action_1_tD69A6DC9FBE94131E52F5A73B2A9D4AB51EEC404  : public MulticastDelegate_t
{
};
struct ArgumentException_tAD90411542A20A9C72D5CDA3A84181D8B947A263  : public SystemException_tCC48D868298F4C0705279823E34B00F4FBDB7295
{
	String_t* ____paramName;
};
struct Deriv_t32B789F19EA5F7301FEE48A702C2B32AF64EF69A  : public MulticastDelegate_t
{
};
struct ArgumentNullException_t327031E412FAB2351B0022DD5DAD47E67E597129  : public ArgumentException_tAD90411542A20A9C72D5CDA3A84181D8B947A263
{
};
struct List_1_tAB32C91F17A9EF1C977FD736177219D0D9DD679A_StaticFields
{
	FlightRiskU5BU5D_t78F88C5B752B50C229A77BF30F51E2426C3209F6* ___s_emptyArray;
};
struct List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D_StaticFields
{
	ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918* ___s_emptyArray;
};
struct List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5_StaticFields
{
	SimEventU5BU5D_t76B06325B23B376D30933AE8A8FB4C34665D72A6* ___s_emptyArray;
};
struct List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8_StaticFields
{
	SimSampleU5BU5D_t5841B807530E6C141503B88A570CA4B3CA13A10B* ___s_emptyArray;
};
struct List_1_tF470A3BE5C1B5B68E1325EF3F109D172E60BD7CD_StaticFields
{
	StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* ___s_emptyArray;
};
struct CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0_StaticFields
{
	CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0* ___invariant_culture_info;
	RuntimeObject* ___shared_table_lock;
	CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0* ___default_current_culture;
	CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0* ___s_DefaultThreadCurrentUICulture;
	CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0* ___s_DefaultThreadCurrentCulture;
	Dictionary_2_t9FA6D82CAFC18769F7515BB51D1C56DAE09381C3* ___shared_by_number;
	Dictionary_2_tE1603CE612C16451D1E56FF4D4859D4FE4087C28* ___shared_by_name;
	CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0* ___s_UserPreferredCultureInfoInAppX;
	bool ___IsTaiwanSku;
};
struct SeededWind_t078A40A35FF4627B5FA2AA1AE748CF01648BD194_StaticFields
{
	SeededWind_t078A40A35FF4627B5FA2AA1AE748CF01648BD194* ___U3CCalmAirU3Ek__BackingField;
};
struct String_t_StaticFields
{
	String_t* ___Empty;
};
struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22_StaticFields
{
	String_t* ___TrueString;
	String_t* ___FalseString;
};
struct IntPtr_t_StaticFields
{
	intptr_t ___Zero;
};
#ifdef __clang__
#pragma clang diagnostic pop
#endif
struct DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE  : public RuntimeArray
{
	ALIGN_FIELD (8) double m_Items[1];

	inline double GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline double* GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, double value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
	}
	inline double GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline double* GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, double value)
	{
		m_Items[index] = value;
	}
};
struct SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641  : public RuntimeArray
{
	ALIGN_FIELD (8) SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* m_Items[1];

	inline SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E** GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
		Il2CppCodeGenWriteBarrier((void**)m_Items + index, (void*)value);
	}
	inline SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E** GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* value)
	{
		m_Items[index] = value;
		Il2CppCodeGenWriteBarrier((void**)m_Items + index, (void*)value);
	}
};
struct ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918  : public RuntimeArray
{
	ALIGN_FIELD (8) RuntimeObject* m_Items[1];

	inline RuntimeObject* GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline RuntimeObject** GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, RuntimeObject* value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
		Il2CppCodeGenWriteBarrier((void**)m_Items + index, (void*)value);
	}
	inline RuntimeObject* GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline RuntimeObject** GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, RuntimeObject* value)
	{
		m_Items[index] = value;
		Il2CppCodeGenWriteBarrier((void**)m_Items + index, (void*)value);
	}
};
struct SimSampleU5BU5D_t5841B807530E6C141503B88A570CA4B3CA13A10B  : public RuntimeArray
{
	ALIGN_FIELD (8) SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121 m_Items[1];

	inline SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121 GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121* GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121 value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
	}
	inline SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121 GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121* GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121 value)
	{
		m_Items[index] = value;
	}
};


IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_gshared_inline (Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5* __this, void* ___0_pointer, int32_t ___1_length, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Action_1__ctor_m1BA854F3F4319EA4A4294DDFDA21C395B8D0FF87_gshared (Action_1_tD69A6DC9FBE94131E52F5A73B2A9D4AB51EEC404* __this, RuntimeObject* ___0_object, intptr_t ___1_method, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool Nullable_1_get_HasValue_mB1F55188CDD50D6D725D41F55D2F2540CD15FB20_gshared_inline (Nullable_1_t163D49A1147F217B7BD43BE8ACC8A5CC6B846D14* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Nullable_1_GetValueOrDefault_m26287DBB36429B2E2FBCB3F96F439248F8CAA8BB_gshared_inline (Nullable_1_t163D49A1147F217B7BD43BE8ACC8A5CC6B846D14* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool Nullable_1_get_HasValue_mCF2FD8B3055FA87FC9C504F2122B3B0FAEDE3EC9_gshared_inline (Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* Nullable_1_ToString_m5FDC0CBE068DB2893454257CE6E29846D47B3038_gshared (Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void ValueTuple_2__ctor_m0856651FDB5D685663AEE4A3D664ACFBB18CF79E_gshared (ValueTuple_2_t6CFD401E32E7EA9EA13C8BA9C5CA4C6BA8F73E5C* __this, RuntimeObject* ___0_item1, double ___1_item2, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void List_1__ctor_m7F078BB342729BDF11327FD89D7872265328F690_gshared (List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void List_1__ctor_mC776F2E1B0BF7A914DD5F7B2DB3E7B333D09A513_gshared (List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Nullable_1__ctor_m141FA88563AC0B5179132FB929EABD02C47FF703_gshared (Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28* __this, int32_t ___0_value, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void List_1_Add_mEBCF994CC3814631017F46A387B1A192ED6C85C7_gshared_inline (List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D* __this, RuntimeObject* ___0_item, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF_gshared (Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165* __this, double ___0_value, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void List_1_Add_m67EF1200FCD49355EECE77EB754C2EED52F8B3DE_gshared_inline (List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8* __this, SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121 ___0_item, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_NO_INLINE IL2CPP_METHOD_ATTR void List_1_AddWithResize_m79A9BF770BEF9C06BE40D5401E55E375F2726CC4_gshared (List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D* __this, RuntimeObject* ___0_item, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_NO_INLINE IL2CPP_METHOD_ATTR void List_1_AddWithResize_m8BA79C0F2A6826CAAA21112A30E06104633D95CC_gshared (List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8* __this, SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121 ___0_item, const RuntimeMethod* method) ;

inline void Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_inline (Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5* __this, void* ___0_pointer, int32_t ___1_length, const RuntimeMethod* method)
{
	((  void (*) (Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5*, void*, int32_t, const RuntimeMethod*))Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_gshared_inline)(__this, ___0_pointer, ___1_length, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Math_Pow_mEAE651F0858203FBE12B72B6A53951BBD0FB5265 (double ___0_x, double ___1_y, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Atmosphere_Geopotential_m87E73EB229D42332F94CF7A6E38E8BF511ABD059 (double ___0_z, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 Atmosphere_Upper_mB094E38380980640FDBA41F933ED555CFD6CB79F (double ___0_z, double ___1_t86, double ___2_p86, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Math_Min_mA3310F1FF7876DA2FC7F37B822E6DD66410565C1 (double ___0_val1, double ___1_val2, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void U3CU3Ec__DisplayClass13_0__ctor_m75AF0DCA50154BA312BF33D3403D7B3F289BD364 (U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void ArgumentNullException__ctor_m444AE141157E333844FC1A9500224C2F9FD24F4B (ArgumentNullException_t327031E412FAB2351B0022DD5DAD47E67E597129* __this, String_t* ___0_paramName, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void ArgumentException__ctor_m026938A67AF9D36BB7ED27F80425D7194B514465 (ArgumentException_tAD90411542A20A9C72D5CDA3A84181D8B947A263* __this, String_t* ___0_message, const RuntimeMethod* method) ;
inline void Action_1__ctor_m1BA854F3F4319EA4A4294DDFDA21C395B8D0FF87 (Action_1_tD69A6DC9FBE94131E52F5A73B2A9D4AB51EEC404* __this, RuntimeObject* ___0_object, intptr_t ___1_method, const RuntimeMethod* method)
{
	((  void (*) (Action_1_tD69A6DC9FBE94131E52F5A73B2A9D4AB51EEC404*, RuntimeObject*, intptr_t, const RuntimeMethod*))Action_1__ctor_m1BA854F3F4319EA4A4294DDFDA21C395B8D0FF87_gshared)(__this, ___0_object, ___1_method, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ParallelLoopResult_t9C383DA248DA9082ECBF057DE0879AB99EE6A734 Parallel_For_mD3332AD2F9C519769F2BEF201C54F3C57EFCF6EC (int32_t ___0_fromInclusive, int32_t ___1_toExclusive, Action_1_tD69A6DC9FBE94131E52F5A73B2A9D4AB51EEC404* ___2_body, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 Atmosphere_At_mA6E038B8FA424E44AD2A97805AC95A264333ECF7 (double ___0_z, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2 (RuntimeObject* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Time_JulianDate_m594F1315B9A1CA84998225A0B0927C391896349F (int32_t ___0_y, int32_t ___1_mo, int32_t ___2_d, double ___3_hours, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Frames_Fmod_mDFE24EF88575C8349A26838B511A8A88FA506D53 (double ___0_x, double ___1_y, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Frames_WrapTwoPi_mEEB242735CB58B363C041EE179669F4033C5C5AA (double ___0_ang, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Frames_EraRad_mF0F9486708B63E13F3B962169AF7BC5CC07F6815 (double ___0_jd1, double ___1_jd2, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Math_Truncate_mE66B1AD68C17D27675DE0CB74643374F9EDB649C (double ___0_d, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Frames_get_Flattening_m542DC04E96C01FEADD5DB997034C42EF80C3F3E9 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR SecularRates_tD523392B109E8AC95C21E767F08383D312C0AB73 Geopotential_Rates_m3794A02018FE5E2FC72FFB20A6AF08A0C9C96103 (double ___0_a, double ___1_e, double ___2_iRad, double ___3_mu, double ___4_re, double ___5_j2, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Math_Max_m7BAC743E1752A51F258BB82DEBDD13E7C6D3ED26 (double ___0_val1, double ___1_val2, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 Kepler_FromState_m5AC5A0E70560CCD179E939EF0787679A64967E13 (double ___0_rx, double ___1_ry, double ___2_rz, double ___3_vx, double ___4_vy, double ___5_vz, double ___6_mu, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Handoff_PeriapsisAltitudeM_mFBB92CD52958C475187A5D94BA1A40FB8B3B8BCD (ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 ___0_el, double ___1_radius, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t Handoff_SelectCoast_mA96BEC1F85020351B41F1DDD0719B0DEFE677B3A (ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 ___0_el, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Kepler_ToState_m90084569889091F70A4B45D0A1426F93C639792A (ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 ___0_el, double* ___1_rx, double* ___2_ry, double* ___3_rz, double* ___4_vx, double* ___5_vy, double* ___6_vz, double ___7_mu, const RuntimeMethod* method) ;
inline bool Nullable_1_get_HasValue_m5292804A8580CD2DD32F958EC5B17A9025B3004A_inline (Nullable_1_t3EE493957A8C36BFB8CFAD3C76358D1249FDDFA0* __this, const RuntimeMethod* method)
{
	return ((  bool (*) (Nullable_1_t3EE493957A8C36BFB8CFAD3C76358D1249FDDFA0*, const RuntimeMethod*))Nullable_1_get_HasValue_mB1F55188CDD50D6D725D41F55D2F2540CD15FB20_gshared_inline)(__this, method);
}
inline int32_t Nullable_1_GetValueOrDefault_mC5602D6D01DDB3D6B4DD87E77F20FA65CFFD00C4_inline (Nullable_1_t3EE493957A8C36BFB8CFAD3C76358D1249FDDFA0* __this, const RuntimeMethod* method)
{
	return ((  int32_t (*) (Nullable_1_t3EE493957A8C36BFB8CFAD3C76358D1249FDDFA0*, const RuntimeMethod*))Nullable_1_GetValueOrDefault_m26287DBB36429B2E2FBCB3F96F439248F8CAA8BB_gshared_inline)(__this, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 Kepler_Propagate_m898EBCF932145F43223C88DC9B1C8F5CBA58D3BA (ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 ___0_el, double ___1_dt, double ___2_mu, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 OnRails_Propagate_mE026D3096F23E7BE2DEC9A9C1F486BD1789648D5 (ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 ___0_el, double ___1_dt, double ___2_mu, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* Object_MemberwiseClone_m0676AEE25C3CF7C09F15ECF9EC5CC407863617B3 (RuntimeObject* __this, const RuntimeMethod* method) ;
inline bool Nullable_1_get_HasValue_mCF2FD8B3055FA87FC9C504F2122B3B0FAEDE3EC9_inline (Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28* __this, const RuntimeMethod* method)
{
	return ((  bool (*) (Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28*, const RuntimeMethod*))Nullable_1_get_HasValue_mCF2FD8B3055FA87FC9C504F2122B3B0FAEDE3EC9_gshared_inline)(__this, method);
}
inline String_t* Nullable_1_ToString_m5FDC0CBE068DB2893454257CE6E29846D47B3038 (Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28* __this, const RuntimeMethod* method)
{
	return ((  String_t* (*) (Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28*, const RuntimeMethod*))Nullable_1_ToString_m5FDC0CBE068DB2893454257CE6E29846D47B3038_gshared)(__this, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* String_Concat_m9E3155FB84015C823606188F53B47CB44C444991 (String_t* ___0_str0, String_t* ___1_str1, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* String_Format_mA0534D6E2AE4D67A6BD8D45B3321323930EB930C (String_t* ___0_format, RuntimeObject* ___1_arg0, RuntimeObject* ___2_arg1, RuntimeObject* ___3_arg2, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* SimStage_Clone_m4F9CB91D7754711D81B1D08F56A98CC361224A27 (SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double RocketEq_MassFlow_mB82582EAD2B0F84192C93DBF5DD78465988C5834 (double ___0_thrust, double ___1_isp, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0* CultureInfo_get_InvariantCulture_mD1E96DC845E34B10F78CB744B0CB5D7D63CEB1E6 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* Double_ToString_m4318830D9F771852FDCF21C14CF9E8ABC7E77357 (double* __this, RuntimeObject* ___0_provider, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Aero_RefArea_m98DD153C9A01FEFA95889E85B5CB928AA579A457 (double ___0_d, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* Headless_Normalize_m0BE85272851F10E3AF5FC646F5218F7C394DAE14 (RuntimeObject* ___0_stages, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double RocketEq_DeltaV_m2BD275655078E8767D0994CE4B961CB5D11AB488 (double ___0_isp, double ___1_m0, double ___2_mf, const RuntimeMethod* method) ;
inline void ValueTuple_2__ctor_m945C088412D024EDD72CDA156A7A63BB5E0D0688 (ValueTuple_2_tCD83B33C76656F32DCB440F5228F21BF25762DE2* __this, DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___0_item1, double ___1_item2, const RuntimeMethod* method)
{
	((  void (*) (ValueTuple_2_tCD83B33C76656F32DCB440F5228F21BF25762DE2*, DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE*, double, const RuntimeMethod*))ValueTuple_2__ctor_m0856651FDB5D685663AEE4A3D664ACFBB18CF79E_gshared)(__this, ___0_item1, ___1_item2, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SimOptions__ctor_mB90E8E2ABA36196352F87ABFBB2D357CDD83E98E (SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void AscentSim__ctor_m817E2F94B05477E9AC311A810BB1700205867B9A (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, RuntimeObject* ___0_stages, SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* ___1_opts, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void AscentSim_set_Sink_m83A341CB52EE9FD70B2BF88750BE6F611CC75295_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, RuntimeObject* ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool AscentSim_Tick_m3AE54AD6763AE540AEC122CE9F9AF31974596B28 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* AscentSim_Finish_m38F11D0801DA64CA9E5EECC1BCB1C1F6A720A6D0 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SimStage__ctor_m52F817B39D240ADCAC5E84FD5668E90C7593D591 (SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool AscentSim_get_Thrusting_m1221CE066B159B7647B0988CCA625853DB547A22_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double AscentSim_StageEndMass_mB2D815CE13FEE7A63EAA69082D706CB616D6C623 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool Double_IsNaN_mF2BC6D1FD4813179B2CAE58D29770E42830D0883_inline (double ___0_d, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RK4__ctor_mC4686CE4A404E704347D2236C13F1BAA316F56DC (RK4_t1BC6735398DED08AF0675CEEB7983FDA7AB140CB* __this, int32_t ___0_n, const RuntimeMethod* method) ;
inline void List_1__ctor_m17F0A7DA25FD5C6456AA73F6E13F0869DB851C82 (List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5* __this, const RuntimeMethod* method)
{
	((  void (*) (List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5*, const RuntimeMethod*))List_1__ctor_m7F078BB342729BDF11327FD89D7872265328F690_gshared)(__this, method);
}
inline void List_1__ctor_mC776F2E1B0BF7A914DD5F7B2DB3E7B333D09A513 (List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8* __this, const RuntimeMethod* method)
{
	((  void (*) (List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8*, const RuntimeMethod*))List_1__ctor_mC776F2E1B0BF7A914DD5F7B2DB3E7B333D09A513_gshared)(__this, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Earth_SiteRotationSpeed_m4408120B6BB6BCD6D9FFEEF64ADF4E24D831FE4B (double ___0_latDeg, double ___1_radius, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void FlightVec__ctor_mCE371A7462C9E8DD33BDB342A3CDF34D1750CFA4 (FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7* __this, double ___0_x, double ___1_y, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Deriv__ctor_m96CA4EA0FBA14F1BA387EA264701F72CE03FCE17 (Deriv_t32B789F19EA5F7301FEE48A702C2B32AF64EF69A* __this, RuntimeObject* ___0_object, intptr_t ___1_method, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SimEvent__ctor_m9601BFE450112DDA042CB7CB0DA82C273CCA7367 (SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* __this, const RuntimeMethod* method) ;
inline void Nullable_1__ctor_m141FA88563AC0B5179132FB929EABD02C47FF703 (Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28* __this, int32_t ___0_value, const RuntimeMethod* method)
{
	((  void (*) (Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28*, int32_t, const RuntimeMethod*))Nullable_1__ctor_m141FA88563AC0B5179132FB929EABD02C47FF703_gshared)(__this, ___0_value, method);
}
inline void List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_inline (List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5* __this, SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* ___0_item, const RuntimeMethod* method)
{
	((  void (*) (List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5*, SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9*, const RuntimeMethod*))List_1_Add_mEBCF994CC3814631017F46A387B1A192ED6C85C7_gshared_inline)(__this, ___0_item, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double SeededWind_Retarget_m66ABCD42DE0C7ED4ED63C5E0F0F9FA063D10A1B1 (SeededWind_t078A40A35FF4627B5FA2AA1AE748CF01648BD194* __this, double ___0_altM, double ___1_tx, double ___2_ty, double* ___3_avx, double* ___4_avy, double ___5_vrelIn, double ___6_avz, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t Math_Min_m53C488772A34D53917BCA2A491E79A0A5356ED52 (int32_t ___0_val1, int32_t ___1_val2, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Aero_CdMach_m0F78C3ADE3138A85FBDF51DC71A3040C035EED47 (double ___0_mach, double ___1_cd0, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double RocketEq_IspAt_m4FFE489F4D4A8AD0D4199B175E2B729E0727C9B6 (double ___0_ispSL, double ___1_ispVac, double ___2_p, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t AscentSim_get_Mode_m5522E937FEF440F09E1F28A50A534066FEA2E2C9_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR double AscentSim_get_PitchCmd_mC76BA7E009D9C12D9F966E247CC7909311A135ED_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool AscentSim_get_Done_mBC8EEA714EDD0073C9F4D91135D515D022B41A6A_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void AscentSim_EnsureIgnition_mD7BE0F38DF384B744ACA8D0BF38AE349D6149F01 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR double AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void AscentSim_set_Done_m95D7993A7D005264297DDDDF34649C09515E702C_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, bool ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double JsMath_Hypot_mACE54445FD52A26990FD1AD7C43FDC73B6F893D4 (double ___0_a, double ___1_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double FailureMonitor_get_ThrustFactor_mCE680846A429E677E27564A6589CC614B226C99A (FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Headless_LegacyPitch_m7901412AEEAE5BE07A9992D2FD9FF0D30EADE09C (GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307* ___0_g, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool AscentSim_HoldingMaxQPitch_m6A1276F1456AD0B8E5EA145533B867A7449BD87B (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, double ___0_alt, double ___1_mass, double ___2_vr, double ___3_vrel, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void AscentSim_set_Mode_m7946FC27FD607C87B37DB52807785705728CFDC3_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, int32_t ___0_value, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void AscentSim_set_PitchCmd_m54C6E65BF6705E8F4DD4DC357074BD0DF8CA677A_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, double ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR GuidanceState_tA40A6AE2010623D64FC40402D0D31113286A0401 AscentSim_Guide_mD6EC3412C6E84F3BB7683B700B76EE6FBA7235E1 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, double ___0_alt, double ___1_m, double ___2_vr, double ___3_g, double ___4_vrel, double ___5_flight, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ValueTuple_2_t6535F6090E6D3C9A7A645F4BFD8ED400CBA8B14C GuidanceProfile_Command_m0569E0B457499A166FC5720399B70D793882CD00 (GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307* __this, GuidanceState_tA40A6AE2010623D64FC40402D0D31113286A0401 ___0_state, const RuntimeMethod* method) ;
inline void Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF (Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165* __this, double ___0_value, const RuntimeMethod* method)
{
	((  void (*) (Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165*, double, const RuntimeMethod*))Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF_gshared)(__this, ___0_value, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool FailureMonitor_OnEmpty_m31D4D1AE7462FA83EC9CA94838F8F2A3E1F7CF0E (FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* __this, double ___0_t, double ___1_alt, bool ___2_last, double ___3_targetAlt, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double AscentSim_Dynamic_m84A8DC43B7B0F22A9CBD7991BB4F2D56A8A6693B (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, double ___0_alt, double ___1_vrel, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool FailureMonitor_OnSeparation_mF74CBB8B5B72CB981F36FED8ED6EB115880E33AD (FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* __this, double ___0_t, double ___1_alt, double ___2_qPa, int32_t ___3_spentIndex, double ___4_massAft, double ___5_massForward, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void AscentSim_Fail_mF30CBB115E75C16458AE75592AA73960EAF8B1F9 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, double ___0_alt, double ___1_speed, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void AscentSim_set_StageIdx_m0D20B990A04C0A885ABBD3A3B4861165D5091CAF_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, int32_t ___0_value, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void AscentSim_set_Thrusting_mD9D054E78DF826C6D731FFE51269A92EBD1D3992_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, bool ___0_value, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR RuntimeObject* AscentSim_get_Sink_m34F4D7A8103A12F8CECC0E1F780FA136EC04C37A_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 AscentSim_Pos_m9934C7D4D6516D15CED891E1F74635B653FF2E8C (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 AscentSim_Vel_m35E2A7777E74DDE08E59164309DA92B57F7E5508 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool AscentSim_Watch_m57EC47ACCA3B4FA1E4ECA4F5865625DFA39EA697 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, double ___0_alt, double ___1_q, double ___2_m, double ___3_vrel, double ___4_p, double ___5_rho, double ___6_sound, double ___7_avx, double ___8_avy, double ___9_ux, double ___10_uy, double ___11_tx, double ___12_ty, double ___13_dt, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void LossAccount_Add_m962FF29EAAF2D6352E3DAC40436A0A57FDFAB859 (double* ___0_gravity, double* ___1_drag, double* ___2_steering, double ___3_radialG, double ___4_vr, double ___5_speed, double ___6_axialAccel, double ___7_thrustCos, double ___8_dragAccel, double ___9_dt, const RuntimeMethod* method) ;
inline void List_1_Add_m67EF1200FCD49355EECE77EB754C2EED52F8B3DE_inline (List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8* __this, SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121 ___0_item, const RuntimeMethod* method)
{
	((  void (*) (List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8*, SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121, const RuntimeMethod*))List_1_Add_m67EF1200FCD49355EECE77EB754C2EED52F8B3DE_gshared_inline)(__this, ___0_item, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RK4_Step_mA370B9F53F0B0CD1473EFDED893537324B37C9A2 (RK4_t1BC6735398DED08AF0675CEEB7983FDA7AB140CB* __this, Deriv_t32B789F19EA5F7301FEE48A702C2B32AF64EF69A* ___0_f, double ___1_t, DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___2_y, double ___3_dt, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void AscentSim_set_T_mECCDAC4D95A8329E97710AAC1A222166080AE6F5_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, double ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool String_op_Inequality_m8C940F3CFC42866709D7CA931B3D77B4BE94BCB6 (String_t* ___0_a, String_t* ___1_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool FailureMonitor_OnImpact_m66B964FF0BF29F5461731342F7087CE498B96415 (FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* __this, double ___0_t, double ___1_sink, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR OrbitElements_t5EEC91C4BE0D34654FC26DBE704E101E14BB7D98 Orbit_Planar_m81894386C1A7EA3C3C48E177AAAF4946DBF669E7 (double ___0_x, double ___1_y, double ___2_vx, double ___3_vy, double ___4_mu, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1 (String_t* ___0_a, String_t* ___1_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool FlightEngineMode_IsEngine_m2F9ADE1AC9768AEBCABC4911DDD4D48C6D2FC6CF (String_t* ___0_kind, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* FlightEngineMode_Of_m83A706E318D08DE99B8A9EC5974EDDA0CEB6D861 (String_t* ___0_kind, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool GuidanceProfile_HoldKickThroughMaxQ_m23A7CC559FC68E146F24CE31276FB6981B1DD06E (GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307* __this, double ___0_q, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool FailureMonitor_Consider_m33EC84A720E446BEF14613644415890C477192BC (FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* __this, Step_t7AA160BDF44184A07EA62B87484E56D55AC7C795* ___0_step, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* Array_Clone_m66C9D0727C9BAA0995E4142F29B45BC03582E042 (RuntimeArray* __this, const RuntimeMethod* method) ;
inline void List_1__ctor_mCA8DD57EAC70C2B5923DBB9D5A77CEAC22E7068E (List_1_tF470A3BE5C1B5B68E1325EF3F109D172E60BD7CD* __this, const RuntimeMethod* method)
{
	((  void (*) (List_1_tF470A3BE5C1B5B68E1325EF3F109D172E60BD7CD*, const RuntimeMethod*))List_1__ctor_m7F078BB342729BDF11327FD89D7872265328F690_gshared)(__this, method);
}
inline void List_1_Add_mF10DB1D3CBB0B14215F0E4F8AB4934A1955E5351_inline (List_1_tF470A3BE5C1B5B68E1325EF3F109D172E60BD7CD* __this, String_t* ___0_item, const RuntimeMethod* method)
{
	((  void (*) (List_1_tF470A3BE5C1B5B68E1325EF3F109D172E60BD7CD*, String_t*, const RuntimeMethod*))List_1_Add_mEBCF994CC3814631017F46A387B1A192ED6C85C7_gshared_inline)(__this, ___0_item, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SimResult__ctor_m6EFFEB4A55446AFAE578555518F726943D7DFD4D (SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ValueTuple_2_tCD83B33C76656F32DCB440F5228F21BF25762DE2 Headless_StackDeltaV_m9C51D98CCE8BDDE9B7F7F6014E1947219FC73C3C (RuntimeObject* ___0_stages, double ___1_payload, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double AscentSim_CoastApexAlt_mC04DF06DBA61D0C0342EA5D61312CCD9A4D5186D (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void FailureMonitor_Finish_mF516837311D82349AEF7B690911B8DB45DE61B6C (FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* __this, double ___0_qPeakPa, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double FailureMonitor_get_MinStability_m9BDC7DF918EBBC3F7CE69DA67F1BD3E4DF72C8A3 (FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double FailureMonitor_get_MinCar_m99D4B873322CA59A57C9EE12A123519B6B408BEA (FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR double FailureMonitor_get_QLimPa_mB3D80483EFA73C221443397E6D960B724E0353FE_inline (FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int64_t BitConverter_DoubleToInt64Bits_m4F42741818550F9956B5FBAF88C051F4DE5B0AE6_inline (double ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void ThrowHelper_ThrowArgumentOutOfRangeException_mD7D90276EDCDF9394A8EA635923E3B48BB71BD56 (const RuntimeMethod* method) ;
inline void List_1_AddWithResize_m79A9BF770BEF9C06BE40D5401E55E375F2726CC4 (List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D* __this, RuntimeObject* ___0_item, const RuntimeMethod* method)
{
	((  void (*) (List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D*, RuntimeObject*, const RuntimeMethod*))List_1_AddWithResize_m79A9BF770BEF9C06BE40D5401E55E375F2726CC4_gshared)(__this, ___0_item, method);
}
inline void List_1_AddWithResize_m8BA79C0F2A6826CAAA21112A30E06104633D95CC (List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8* __this, SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121 ___0_item, const RuntimeMethod* method)
{
	((  void (*) (List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8*, SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121, const RuntimeMethod*))List_1_AddWithResize_m8BA79C0F2A6826CAAA21112A30E06104633D95CC_gshared)(__this, ___0_item, method);
}
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Atmosphere_Geopotential_m87E73EB229D42332F94CF7A6E38E8BF511ABD059 (double ___0_z, const RuntimeMethod* method) 
{
	{
		double L_0 = ___0_z;
		double L_1 = ___0_z;
		return ((double)(((double)il2cpp_codegen_multiply((6356766.0), L_0))/((double)il2cpp_codegen_add((6356766.0), L_1))));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 Atmosphere_At_mA6E038B8FA424E44AD2A97805AC95A264333ECF7 (double ___0_z, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_RuntimeMethod_var);
		s_Il2CppMethodInitialized = true;
	}
	Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5 V_0;
	memset((&V_0), 0, sizeof(V_0));
	Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5 V_1;
	memset((&V_1), 0, sizeof(V_1));
	Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5 V_2;
	memset((&V_2), 0, sizeof(V_2));
	Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5 V_3;
	memset((&V_3), 0, sizeof(V_3));
	double V_4 = 0.0;
	double V_5 = 0.0;
	int32_t V_6 = 0;
	double V_7 = 0.0;
	double V_8 = 0.0;
	double V_9 = 0.0;
	int32_t V_10 = 0;
	double V_11 = 0.0;
	double V_12 = 0.0;
	AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 V_13;
	memset((&V_13), 0, sizeof(V_13));
	double* G_B3_0 = NULL;
	double* G_B2_0 = NULL;
	double G_B4_0 = 0.0;
	double* G_B4_1 = NULL;
	double G_B9_0 = 0.0;
	double G_B16_0 = 0.0;
	{
		uintptr_t L_0 = ((uintptr_t)((int32_t)64));
		int8_t* L_1 = (int8_t*) (L_0 ? alloca(L_0) : NULL);
		memset(L_1, 0, L_0);
		int8_t* L_2 = (L_1);
		*((double*)L_2) = (double)(0.0);
		int8_t* L_3 = L_2;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_3, 8))) = (double)(11000.0);
		int8_t* L_4 = L_3;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_4, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)2), 8))))) = (double)(20000.0);
		int8_t* L_5 = L_4;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_5, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)3), 8))))) = (double)(32000.0);
		int8_t* L_6 = L_5;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_6, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)4), 8))))) = (double)(47000.0);
		int8_t* L_7 = L_6;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_7, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)5), 8))))) = (double)(51000.0);
		int8_t* L_8 = L_7;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_8, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)6), 8))))) = (double)(71000.0);
		int8_t* L_9 = L_8;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_9, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)7), 8))))) = (double)(84852.0);
		Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5 L_10;
		memset((&L_10), 0, sizeof(L_10));
		Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_inline((&L_10), (void*)L_9, 8, Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_RuntimeMethod_var);
		V_0 = L_10;
		uintptr_t L_11 = ((uintptr_t)((int32_t)64));
		int8_t* L_12 = (int8_t*) (L_11 ? alloca(L_11) : NULL);
		memset(L_12, 0, L_11);
		int8_t* L_13 = (L_12);
		*((double*)L_13) = (double)(-0.0064999999999999997);
		int8_t* L_14 = L_13;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_14, 8))) = (double)(0.0);
		int8_t* L_15 = L_14;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_15, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)2), 8))))) = (double)(0.001);
		int8_t* L_16 = L_15;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_16, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)3), 8))))) = (double)(0.0028);
		int8_t* L_17 = L_16;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_17, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)4), 8))))) = (double)(0.0);
		int8_t* L_18 = L_17;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_18, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)5), 8))))) = (double)(-0.0028);
		int8_t* L_19 = L_18;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_19, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)6), 8))))) = (double)(-0.002);
		int8_t* L_20 = L_19;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_20, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)7), 8))))) = (double)(0.0);
		Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5 L_21;
		memset((&L_21), 0, sizeof(L_21));
		Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_inline((&L_21), (void*)L_20, 8, Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_RuntimeMethod_var);
		V_1 = L_21;
		uintptr_t L_22 = ((uintptr_t)((int32_t)64));
		int8_t* L_23 = (int8_t*) (L_22 ? alloca(L_22) : NULL);
		memset(L_23, 0, L_22);
		Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5 L_24;
		memset((&L_24), 0, sizeof(L_24));
		Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_inline((&L_24), (void*)(L_23), 8, Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_RuntimeMethod_var);
		V_2 = L_24;
		uintptr_t L_25 = ((uintptr_t)((int32_t)64));
		int8_t* L_26 = (int8_t*) (L_25 ? alloca(L_25) : NULL);
		memset(L_26, 0, L_25);
		Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5 L_27;
		memset((&L_27), 0, sizeof(L_27));
		Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_inline((&L_27), (void*)(L_26), 8, Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_RuntimeMethod_var);
		V_3 = L_27;
		double* L_28;
		L_28 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_2))->____pointer))->value, (0), ((&V_2))->____length);
		*((double*)L_28) = (double)(288.14999999999998);
		double* L_29;
		L_29 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_3))->____pointer))->value, (0), ((&V_3))->____length);
		*((double*)L_29) = (double)(101325.0);
		V_6 = 1;
		goto IL_0222;
	}

IL_014c:
	{
		int32_t L_30 = V_6;
		double* L_31;
		L_31 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_0))->____pointer))->value, (L_30), ((&V_0))->____length);
		double L_32 = *((double*)L_31);
		int32_t L_33 = V_6;
		double* L_34;
		L_34 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_0))->____pointer))->value, (((int32_t)il2cpp_codegen_subtract(L_33, 1))), ((&V_0))->____length);
		double L_35 = *((double*)L_34);
		V_7 = ((double)il2cpp_codegen_subtract(L_32, L_35));
		int32_t L_36 = V_6;
		double* L_37;
		L_37 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_1))->____pointer))->value, (((int32_t)il2cpp_codegen_subtract(L_36, 1))), ((&V_1))->____length);
		double L_38 = *((double*)L_37);
		V_8 = L_38;
		int32_t L_39 = V_6;
		double* L_40;
		L_40 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_2))->____pointer))->value, (L_39), ((&V_2))->____length);
		int32_t L_41 = V_6;
		double* L_42;
		L_42 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_2))->____pointer))->value, (((int32_t)il2cpp_codegen_subtract(L_41, 1))), ((&V_2))->____length);
		double L_43 = *((double*)L_42);
		double L_44 = V_8;
		double L_45 = V_7;
		*((double*)L_40) = (double)((double)il2cpp_codegen_add(L_43, ((double)il2cpp_codegen_multiply(L_44, L_45))));
		int32_t L_46 = V_6;
		double* L_47;
		L_47 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_3))->____pointer))->value, (L_46), ((&V_3))->____length);
		double L_48 = V_8;
		if ((((double)L_48) == ((double)(0.0))))
		{
			G_B3_0 = L_47;
			goto IL_01e6;
		}
		G_B2_0 = L_47;
	}
	{
		int32_t L_49 = V_6;
		double* L_50;
		L_50 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_3))->____pointer))->value, (((int32_t)il2cpp_codegen_subtract(L_49, 1))), ((&V_3))->____length);
		double L_51 = *((double*)L_50);
		int32_t L_52 = V_6;
		double* L_53;
		L_53 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_2))->____pointer))->value, (L_52), ((&V_2))->____length);
		double L_54 = *((double*)L_53);
		int32_t L_55 = V_6;
		double* L_56;
		L_56 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_2))->____pointer))->value, (((int32_t)il2cpp_codegen_subtract(L_55, 1))), ((&V_2))->____length);
		double L_57 = *((double*)L_56);
		double L_58 = V_8;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_59;
		L_59 = Math_Pow_mEAE651F0858203FBE12B72B6A53951BBD0FB5265(((double)(L_54/L_57)), ((double)((-9.8066499999999994)/((double)il2cpp_codegen_multiply((287.053), L_58)))), NULL);
		G_B4_0 = ((double)il2cpp_codegen_multiply(L_51, L_59));
		G_B4_1 = G_B2_0;
		goto IL_021b;
	}

IL_01e6:
	{
		int32_t L_60 = V_6;
		double* L_61;
		L_61 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_3))->____pointer))->value, (((int32_t)il2cpp_codegen_subtract(L_60, 1))), ((&V_3))->____length);
		double L_62 = *((double*)L_61);
		double L_63 = V_7;
		int32_t L_64 = V_6;
		double* L_65;
		L_65 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_2))->____pointer))->value, (((int32_t)il2cpp_codegen_subtract(L_64, 1))), ((&V_2))->____length);
		double L_66 = *((double*)L_65);
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_67;
		L_67 = exp(((double)(((double)il2cpp_codegen_multiply((-9.8066499999999994), L_63))/((double)il2cpp_codegen_multiply((287.053), L_66)))));
		G_B4_0 = ((double)il2cpp_codegen_multiply(L_62, L_67));
		G_B4_1 = G_B3_0;
	}

IL_021b:
	{
		*((double*)G_B4_1) = (double)G_B4_0;
		int32_t L_68 = V_6;
		V_6 = ((int32_t)il2cpp_codegen_add(L_68, 1));
	}

IL_0222:
	{
		int32_t L_69 = V_6;
		if ((((int32_t)L_69) < ((int32_t)8)))
		{
			goto IL_014c;
		}
	}
	{
		double L_70 = ___0_z;
		if ((((double)L_70) > ((double)(86000.0))))
		{
			goto IL_0239;
		}
	}
	{
		double L_71 = ___0_z;
		G_B9_0 = L_71;
		goto IL_0242;
	}

IL_0239:
	{
		G_B9_0 = (86000.0);
	}

IL_0242:
	{
		double L_72;
		L_72 = Atmosphere_Geopotential_m87E73EB229D42332F94CF7A6E38E8BF511ABD059(G_B9_0, NULL);
		V_9 = L_72;
		V_10 = 0;
		goto IL_0254;
	}

IL_024e:
	{
		int32_t L_73 = V_10;
		V_10 = ((int32_t)il2cpp_codegen_add(L_73, 1));
	}

IL_0254:
	{
		int32_t L_74 = V_10;
		if ((((int32_t)L_74) >= ((int32_t)7)))
		{
			goto IL_0269;
		}
	}
	{
		double L_75 = V_9;
		int32_t L_76 = V_10;
		double* L_77;
		L_77 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_0))->____pointer))->value, (((int32_t)il2cpp_codegen_add(L_76, 1))), ((&V_0))->____length);
		double L_78 = *((double*)L_77);
		if ((((double)L_75) >= ((double)L_78)))
		{
			goto IL_024e;
		}
	}

IL_0269:
	{
		int32_t L_79 = V_10;
		double* L_80;
		L_80 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_1))->____pointer))->value, (L_79), ((&V_1))->____length);
		double L_81 = *((double*)L_80);
		V_11 = L_81;
		double L_82 = V_9;
		int32_t L_83 = V_10;
		double* L_84;
		L_84 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_0))->____pointer))->value, (L_83), ((&V_0))->____length);
		double L_85 = *((double*)L_84);
		V_12 = ((double)il2cpp_codegen_subtract(L_82, L_85));
		int32_t L_86 = V_10;
		double* L_87;
		L_87 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_2))->____pointer))->value, (L_86), ((&V_2))->____length);
		double L_88 = *((double*)L_87);
		double L_89 = V_11;
		double L_90 = V_12;
		V_4 = ((double)il2cpp_codegen_add(L_88, ((double)il2cpp_codegen_multiply(L_89, L_90))));
		double L_91 = V_11;
		if ((((double)L_91) == ((double)(0.0))))
		{
			goto IL_02d8;
		}
	}
	{
		int32_t L_92 = V_10;
		double* L_93;
		L_93 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_3))->____pointer))->value, (L_92), ((&V_3))->____length);
		double L_94 = *((double*)L_93);
		double L_95 = V_4;
		int32_t L_96 = V_10;
		double* L_97;
		L_97 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_2))->____pointer))->value, (L_96), ((&V_2))->____length);
		double L_98 = *((double*)L_97);
		double L_99 = V_11;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_100;
		L_100 = Math_Pow_mEAE651F0858203FBE12B72B6A53951BBD0FB5265(((double)(L_95/L_98)), ((double)((-9.8066499999999994)/((double)il2cpp_codegen_multiply((287.053), L_99)))), NULL);
		G_B16_0 = ((double)il2cpp_codegen_multiply(L_94, L_100));
		goto IL_0309;
	}

IL_02d8:
	{
		int32_t L_101 = V_10;
		double* L_102;
		L_102 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_3))->____pointer))->value, (L_101), ((&V_3))->____length);
		double L_103 = *((double*)L_102);
		double L_104 = V_12;
		int32_t L_105 = V_10;
		double* L_106;
		L_106 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_2))->____pointer))->value, (L_105), ((&V_2))->____length);
		double L_107 = *((double*)L_106);
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_108;
		L_108 = exp(((double)(((double)il2cpp_codegen_multiply((-9.8066499999999994), L_104))/((double)il2cpp_codegen_multiply((287.053), L_107)))));
		G_B16_0 = ((double)il2cpp_codegen_multiply(L_103, L_108));
	}

IL_0309:
	{
		V_5 = G_B16_0;
		double L_109 = ___0_z;
		if ((!(((double)L_109) > ((double)(86000.0)))))
		{
			goto IL_0322;
		}
	}
	{
		double L_110 = ___0_z;
		double L_111 = V_4;
		double L_112 = V_5;
		AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 L_113;
		L_113 = Atmosphere_Upper_mB094E38380980640FDBA41F933ED555CFD6CB79F(L_110, L_111, L_112, NULL);
		return L_113;
	}

IL_0322:
	{
		il2cpp_codegen_initobj((&V_13), sizeof(AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1));
		double L_114 = V_4;
		(&V_13)->___T = L_114;
		double L_115 = V_5;
		(&V_13)->___p = L_115;
		double L_116 = V_5;
		double L_117 = V_4;
		(&V_13)->___rho = ((double)(L_116/((double)il2cpp_codegen_multiply((287.053), L_117))));
		double L_118 = V_4;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_119;
		L_119 = sqrt(((double)il2cpp_codegen_multiply((401.87419999999997), L_118)));
		(&V_13)->___a = L_119;
		AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 L_120 = V_13;
		return L_120;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 Atmosphere_Upper_mB094E38380980640FDBA41F933ED555CFD6CB79F (double ___0_z, double ___1_t86, double ___2_p86, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_RuntimeMethod_var);
		s_Il2CppMethodInitialized = true;
	}
	Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5 V_0;
	memset((&V_0), 0, sizeof(V_0));
	Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5 V_1;
	memset((&V_1), 0, sizeof(V_1));
	Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5 V_2;
	memset((&V_2), 0, sizeof(V_2));
	Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5 V_3;
	memset((&V_3), 0, sizeof(V_3));
	double V_4 = 0.0;
	double V_5 = 0.0;
	double V_6 = 0.0;
	double V_7 = 0.0;
	double V_8 = 0.0;
	int32_t V_9 = 0;
	double V_10 = 0.0;
	double V_11 = 0.0;
	double V_12 = 0.0;
	int32_t V_13 = 0;
	AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 V_14;
	memset((&V_14), 0, sizeof(V_14));
	{
		uintptr_t L_0 = ((uintptr_t)((int32_t)1464));
		int8_t* L_1 = (int8_t*) (L_0 ? alloca(L_0) : NULL);
		memset(L_1, 0, L_0);
		int8_t* L_2 = (L_1);
		*((double*)L_2) = (double)(186.86699999999999);
		int8_t* L_3 = L_2;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_3, 8))) = (double)(188.41800000000001);
		int8_t* L_4 = L_3;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_4, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)2), 8))))) = (double)(195.08099999999999);
		int8_t* L_5 = L_4;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_5, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)3), 8))))) = (double)(208.83500000000001);
		int8_t* L_6 = L_5;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_6, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)4), 8))))) = (double)(240.0);
		int8_t* L_7 = L_6;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_7, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)5), 8))))) = (double)(300.0);
		int8_t* L_8 = L_7;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_8, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)6), 8))))) = (double)(360.0);
		int8_t* L_9 = L_8;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_9, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)7), 8))))) = (double)(417.23099999999999);
		int8_t* L_10 = L_9;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_10, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)8), 8))))) = (double)(469.26799999999997);
		int8_t* L_11 = L_10;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_11, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)9)), 8))))) = (double)(516.58900000000006);
		int8_t* L_12 = L_11;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_12, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)10)), 8))))) = (double)(559.62699999999995);
		int8_t* L_13 = L_12;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_13, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)11)), 8))))) = (double)(598.77599999999995);
		int8_t* L_14 = L_13;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_14, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)12)), 8))))) = (double)(634.39200000000005);
		int8_t* L_15 = L_14;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_15, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)13)), 8))))) = (double)(666.79899999999998);
		int8_t* L_16 = L_15;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_16, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)14)), 8))))) = (double)(696.28999999999996);
		int8_t* L_17 = L_16;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_17, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)15)), 8))))) = (double)(723.13199999999995);
		int8_t* L_18 = L_17;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_18, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)16)), 8))))) = (double)(747.56600000000003);
		int8_t* L_19 = L_18;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_19, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)17)), 8))))) = (double)(769.81100000000004);
		int8_t* L_20 = L_19;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_20, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)18)), 8))))) = (double)(790.06600000000003);
		int8_t* L_21 = L_20;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_21, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)19)), 8))))) = (double)(808.51099999999997);
		int8_t* L_22 = L_21;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_22, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)20)), 8))))) = (double)(825.31200000000001);
		int8_t* L_23 = L_22;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_23, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)21)), 8))))) = (double)(840.61599999999999);
		int8_t* L_24 = L_23;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_24, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)22)), 8))))) = (double)(854.55899999999997);
		int8_t* L_25 = L_24;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_25, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)23)), 8))))) = (double)(867.26400000000001);
		int8_t* L_26 = L_25;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_26, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)24)), 8))))) = (double)(878.84199999999998);
		int8_t* L_27 = L_26;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_27, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)25)), 8))))) = (double)(889.39499999999998);
		int8_t* L_28 = L_27;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_28, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)26)), 8))))) = (double)(899.01400000000001);
		int8_t* L_29 = L_28;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_29, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)27)), 8))))) = (double)(907.78499999999997);
		int8_t* L_30 = L_29;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_30, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)28)), 8))))) = (double)(915.78200000000004);
		int8_t* L_31 = L_30;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_31, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)29)), 8))))) = (double)(923.07500000000005);
		int8_t* L_32 = L_31;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_32, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)30)), 8))))) = (double)(929.726);
		int8_t* L_33 = L_32;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_33, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)31)), 8))))) = (double)(935.79399999999998);
		int8_t* L_34 = L_33;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_34, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)32)), 8))))) = (double)(941.33000000000004);
		int8_t* L_35 = L_34;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_35, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)33)), 8))))) = (double)(946.38099999999997);
		int8_t* L_36 = L_35;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_36, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)34)), 8))))) = (double)(950.99099999999999);
		int8_t* L_37 = L_36;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_37, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)35)), 8))))) = (double)(955.19799999999998);
		int8_t* L_38 = L_37;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_38, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)36)), 8))))) = (double)(959.03899999999999);
		int8_t* L_39 = L_38;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_39, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)37)), 8))))) = (double)(962.54499999999996);
		int8_t* L_40 = L_39;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_40, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)38)), 8))))) = (double)(965.74599999999998);
		int8_t* L_41 = L_40;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_41, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)39)), 8))))) = (double)(968.66999999999996);
		int8_t* L_42 = L_41;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_42, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)40)), 8))))) = (double)(971.34000000000003);
		int8_t* L_43 = L_42;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_43, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)41)), 8))))) = (double)(973.779);
		int8_t* L_44 = L_43;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_44, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)42)), 8))))) = (double)(976.00800000000004);
		int8_t* L_45 = L_44;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_45, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)43)), 8))))) = (double)(978.04399999999998);
		int8_t* L_46 = L_45;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_46, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)44)), 8))))) = (double)(979.904);
		int8_t* L_47 = L_46;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_47, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)45)), 8))))) = (double)(981.60500000000002);
		int8_t* L_48 = L_47;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_48, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)46)), 8))))) = (double)(983.15899999999999);
		int8_t* L_49 = L_48;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_49, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)47)), 8))))) = (double)(984.58000000000004);
		int8_t* L_50 = L_49;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_50, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)48)), 8))))) = (double)(985.88);
		int8_t* L_51 = L_50;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_51, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)49)), 8))))) = (double)(987.06799999999998);
		int8_t* L_52 = L_51;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_52, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)50)), 8))))) = (double)(988.154);
		int8_t* L_53 = L_52;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_53, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)51)), 8))))) = (double)(989.14800000000002);
		int8_t* L_54 = L_53;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_54, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)52)), 8))))) = (double)(990.05700000000002);
		int8_t* L_55 = L_54;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_55, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)53)), 8))))) = (double)(990.88900000000001);
		int8_t* L_56 = L_55;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_56, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)54)), 8))))) = (double)(991.64999999999998);
		int8_t* L_57 = L_56;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_57, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)55)), 8))))) = (double)(992.34699999999998);
		int8_t* L_58 = L_57;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_58, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)56)), 8))))) = (double)(992.98400000000004);
		int8_t* L_59 = L_58;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_59, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)57)), 8))))) = (double)(993.56799999999998);
		int8_t* L_60 = L_59;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_60, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)58)), 8))))) = (double)(994.10199999999998);
		int8_t* L_61 = L_60;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_61, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)59)), 8))))) = (double)(994.59100000000001);
		int8_t* L_62 = L_61;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_62, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)60)), 8))))) = (double)(995.03899999999999);
		int8_t* L_63 = L_62;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_63, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)61)), 8))))) = (double)(995.45000000000005);
		int8_t* L_64 = L_63;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_64, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)62)), 8))))) = (double)(995.82500000000005);
		int8_t* L_65 = L_64;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_65, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)63)), 8))))) = (double)(996.16999999999996);
		int8_t* L_66 = L_65;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_66, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)64)), 8))))) = (double)(996.48500000000001);
		int8_t* L_67 = L_66;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_67, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)65)), 8))))) = (double)(996.774);
		int8_t* L_68 = L_67;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_68, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)66)), 8))))) = (double)(997.03899999999999);
		int8_t* L_69 = L_68;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_69, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)67)), 8))))) = (double)(997.28200000000004);
		int8_t* L_70 = L_69;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_70, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)68)), 8))))) = (double)(997.505);
		int8_t* L_71 = L_70;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_71, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)69)), 8))))) = (double)(997.70899999999995);
		int8_t* L_72 = L_71;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_72, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)70)), 8))))) = (double)(997.89599999999996);
		int8_t* L_73 = L_72;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_73, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)71)), 8))))) = (double)(998.06700000000001);
		int8_t* L_74 = L_73;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_74, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)72)), 8))))) = (double)(998.22500000000002);
		int8_t* L_75 = L_74;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_75, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)73)), 8))))) = (double)(998.36900000000003);
		int8_t* L_76 = L_75;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_76, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)74)), 8))))) = (double)(998.50199999999995);
		int8_t* L_77 = L_76;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_77, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)75)), 8))))) = (double)(998.62300000000005);
		int8_t* L_78 = L_77;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_78, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)76)), 8))))) = (double)(998.73500000000001);
		int8_t* L_79 = L_78;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_79, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)77)), 8))))) = (double)(998.83699999999999);
		int8_t* L_80 = L_79;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_80, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)78)), 8))))) = (double)(998.93100000000004);
		int8_t* L_81 = L_80;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_81, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)79)), 8))))) = (double)(999.01700000000005);
		int8_t* L_82 = L_81;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_82, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)80)), 8))))) = (double)(999.096);
		int8_t* L_83 = L_82;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_83, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)81)), 8))))) = (double)(999.16899999999998);
		int8_t* L_84 = L_83;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_84, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)82)), 8))))) = (double)(999.23599999999999);
		int8_t* L_85 = L_84;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_85, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)83)), 8))))) = (double)(999.29700000000003);
		int8_t* L_86 = L_85;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_86, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)84)), 8))))) = (double)(999.35299999999995);
		int8_t* L_87 = L_86;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_87, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)85)), 8))))) = (double)(999.40499999999997);
		int8_t* L_88 = L_87;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_88, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)86)), 8))))) = (double)(999.452);
		int8_t* L_89 = L_88;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_89, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)87)), 8))))) = (double)(999.49599999999998);
		int8_t* L_90 = L_89;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_90, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)88)), 8))))) = (double)(999.53599999999994);
		int8_t* L_91 = L_90;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_91, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)89)), 8))))) = (double)(999.57299999999998);
		int8_t* L_92 = L_91;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_92, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)90)), 8))))) = (double)(999.60699999999997);
		int8_t* L_93 = L_92;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_93, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)91)), 8))))) = (double)(999.63800000000003);
		int8_t* L_94 = L_93;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_94, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)92)), 8))))) = (double)(999.66700000000003);
		int8_t* L_95 = L_94;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_95, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)93)), 8))))) = (double)(999.69299999999998);
		int8_t* L_96 = L_95;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_96, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)94)), 8))))) = (double)(999.71699999999998);
		int8_t* L_97 = L_96;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_97, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)95)), 8))))) = (double)(999.74000000000001);
		int8_t* L_98 = L_97;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_98, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)96)), 8))))) = (double)(999.75999999999999);
		int8_t* L_99 = L_98;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_99, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)97)), 8))))) = (double)(999.779);
		int8_t* L_100 = L_99;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_100, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)98)), 8))))) = (double)(999.79600000000005);
		int8_t* L_101 = L_100;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_101, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)99)), 8))))) = (double)(999.81200000000001);
		int8_t* L_102 = L_101;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_102, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)100)), 8))))) = (double)(999.827);
		int8_t* L_103 = L_102;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_103, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)101)), 8))))) = (double)(999.84100000000001);
		int8_t* L_104 = L_103;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_104, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)102)), 8))))) = (double)(999.85299999999995);
		int8_t* L_105 = L_104;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_105, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)103)), 8))))) = (double)(999.86400000000003);
		int8_t* L_106 = L_105;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_106, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)104)), 8))))) = (double)(999.875);
		int8_t* L_107 = L_106;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_107, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)105)), 8))))) = (double)(999.88499999999999);
		int8_t* L_108 = L_107;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_108, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)106)), 8))))) = (double)(999.89400000000001);
		int8_t* L_109 = L_108;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_109, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)107)), 8))))) = (double)(999.90200000000004);
		int8_t* L_110 = L_109;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_110, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)108)), 8))))) = (double)(999.90999999999997);
		int8_t* L_111 = L_110;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_111, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)109)), 8))))) = (double)(999.91700000000003);
		int8_t* L_112 = L_111;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_112, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)110)), 8))))) = (double)(999.923);
		int8_t* L_113 = L_112;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_113, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)111)), 8))))) = (double)(999.92899999999997);
		int8_t* L_114 = L_113;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_114, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)112)), 8))))) = (double)(999.93399999999997);
		int8_t* L_115 = L_114;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_115, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)113)), 8))))) = (double)(999.93899999999996);
		int8_t* L_116 = L_115;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_116, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)114)), 8))))) = (double)(999.94399999999996);
		int8_t* L_117 = L_116;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_117, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)115)), 8))))) = (double)(999.94799999999998);
		int8_t* L_118 = L_117;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_118, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)116)), 8))))) = (double)(999.952);
		int8_t* L_119 = L_118;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_119, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)117)), 8))))) = (double)(999.95600000000002);
		int8_t* L_120 = L_119;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_120, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)118)), 8))))) = (double)(999.95899999999995);
		int8_t* L_121 = L_120;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_121, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)119)), 8))))) = (double)(999.96199999999999);
		int8_t* L_122 = L_121;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_122, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)120)), 8))))) = (double)(999.96500000000003);
		int8_t* L_123 = L_122;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_123, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)121)), 8))))) = (double)(999.96799999999996);
		int8_t* L_124 = L_123;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_124, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)122)), 8))))) = (double)(999.97000000000003);
		int8_t* L_125 = L_124;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_125, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)123)), 8))))) = (double)(999.97299999999996);
		int8_t* L_126 = L_125;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_126, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)124)), 8))))) = (double)(999.97500000000002);
		int8_t* L_127 = L_126;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_127, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)125)), 8))))) = (double)(999.97699999999998);
		int8_t* L_128 = L_127;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_128, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)126)), 8))))) = (double)(999.97799999999995);
		int8_t* L_129 = L_128;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_129, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)127)), 8))))) = (double)(999.98000000000002);
		int8_t* L_130 = L_129;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_130, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)128)), 8))))) = (double)(999.98199999999997);
		int8_t* L_131 = L_130;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_131, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)129)), 8))))) = (double)(999.98299999999995);
		int8_t* L_132 = L_131;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_132, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)130)), 8))))) = (double)(999.98400000000004);
		int8_t* L_133 = L_132;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_133, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)131)), 8))))) = (double)(999.98500000000001);
		int8_t* L_134 = L_133;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_134, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)132)), 8))))) = (double)(999.98599999999999);
		int8_t* L_135 = L_134;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_135, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)133)), 8))))) = (double)(999.98699999999997);
		int8_t* L_136 = L_135;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_136, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)134)), 8))))) = (double)(999.98800000000006);
		int8_t* L_137 = L_136;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_137, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)135)), 8))))) = (double)(999.98900000000003);
		int8_t* L_138 = L_137;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_138, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)136)), 8))))) = (double)(999.99000000000001);
		int8_t* L_139 = L_138;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_139, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)137)), 8))))) = (double)(999.99099999999999);
		int8_t* L_140 = L_139;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_140, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)138)), 8))))) = (double)(999.99199999999996);
		int8_t* L_141 = L_140;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_141, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)139)), 8))))) = (double)(999.99199999999996);
		int8_t* L_142 = L_141;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_142, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)140)), 8))))) = (double)(999.99300000000005);
		int8_t* L_143 = L_142;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_143, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)141)), 8))))) = (double)(999.99300000000005);
		int8_t* L_144 = L_143;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_144, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)142)), 8))))) = (double)(999.99400000000003);
		int8_t* L_145 = L_144;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_145, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)143)), 8))))) = (double)(999.99400000000003);
		int8_t* L_146 = L_145;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_146, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)144)), 8))))) = (double)(999.995);
		int8_t* L_147 = L_146;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_147, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)145)), 8))))) = (double)(999.995);
		int8_t* L_148 = L_147;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_148, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)146)), 8))))) = (double)(999.995);
		int8_t* L_149 = L_148;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_149, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)147)), 8))))) = (double)(999.99599999999998);
		int8_t* L_150 = L_149;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_150, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)148)), 8))))) = (double)(999.99599999999998);
		int8_t* L_151 = L_150;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_151, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)149)), 8))))) = (double)(999.99599999999998);
		int8_t* L_152 = L_151;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_152, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)150)), 8))))) = (double)(999.99699999999996);
		int8_t* L_153 = L_152;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_153, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)151)), 8))))) = (double)(999.99699999999996);
		int8_t* L_154 = L_153;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_154, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)152)), 8))))) = (double)(999.99699999999996);
		int8_t* L_155 = L_154;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_155, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)153)), 8))))) = (double)(999.99699999999996);
		int8_t* L_156 = L_155;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_156, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)154)), 8))))) = (double)(999.99699999999996);
		int8_t* L_157 = L_156;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_157, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)155)), 8))))) = (double)(999.99800000000005);
		int8_t* L_158 = L_157;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_158, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)156)), 8))))) = (double)(999.99800000000005);
		int8_t* L_159 = L_158;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_159, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)157)), 8))))) = (double)(999.99800000000005);
		int8_t* L_160 = L_159;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_160, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)158)), 8))))) = (double)(999.99800000000005);
		int8_t* L_161 = L_160;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_161, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)159)), 8))))) = (double)(999.99800000000005);
		int8_t* L_162 = L_161;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_162, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)160)), 8))))) = (double)(999.99800000000005);
		int8_t* L_163 = L_162;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_163, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)161)), 8))))) = (double)(999.99900000000002);
		int8_t* L_164 = L_163;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_164, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)162)), 8))))) = (double)(999.99900000000002);
		int8_t* L_165 = L_164;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_165, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)163)), 8))))) = (double)(999.99900000000002);
		int8_t* L_166 = L_165;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_166, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)164)), 8))))) = (double)(999.99900000000002);
		int8_t* L_167 = L_166;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_167, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)165)), 8))))) = (double)(999.99900000000002);
		int8_t* L_168 = L_167;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_168, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)166)), 8))))) = (double)(999.99900000000002);
		int8_t* L_169 = L_168;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_169, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)167)), 8))))) = (double)(999.99900000000002);
		int8_t* L_170 = L_169;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_170, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)168)), 8))))) = (double)(999.99900000000002);
		int8_t* L_171 = L_170;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_171, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)169)), 8))))) = (double)(999.99900000000002);
		int8_t* L_172 = L_171;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_172, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)170)), 8))))) = (double)(999.99900000000002);
		int8_t* L_173 = L_172;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_173, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)171)), 8))))) = (double)(999.99900000000002);
		int8_t* L_174 = L_173;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_174, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)172)), 8))))) = (double)(999.99900000000002);
		int8_t* L_175 = L_174;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_175, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)173)), 8))))) = (double)(999.99900000000002);
		int8_t* L_176 = L_175;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_176, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)174)), 8))))) = (double)(999.99900000000002);
		int8_t* L_177 = L_176;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_177, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)175)), 8))))) = (double)(999.99900000000002);
		int8_t* L_178 = L_177;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_178, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)176)), 8))))) = (double)(1000.0);
		int8_t* L_179 = L_178;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_179, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)177)), 8))))) = (double)(1000.0);
		int8_t* L_180 = L_179;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_180, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)178)), 8))))) = (double)(1000.0);
		int8_t* L_181 = L_180;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_181, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)179)), 8))))) = (double)(1000.0);
		int8_t* L_182 = L_181;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_182, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)180)), 8))))) = (double)(1000.0);
		int8_t* L_183 = L_182;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_183, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)181)), 8))))) = (double)(1000.0);
		int8_t* L_184 = L_183;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_184, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)182)), 8))))) = (double)(1000.0);
		Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5 L_185;
		memset((&L_185), 0, sizeof(L_185));
		Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_inline((&L_185), (void*)L_184, ((int32_t)183), Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_RuntimeMethod_var);
		V_0 = L_185;
		uintptr_t L_186 = ((uintptr_t)((int32_t)1464));
		int8_t* L_187 = (int8_t*) (L_186 ? alloca(L_186) : NULL);
		memset(L_187, 0, L_186);
		int8_t* L_188 = (L_187);
		*((double*)L_188) = (double)(0.18435000000000001);
		int8_t* L_189 = L_188;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_189, 8))) = (double)(0.075774999999999995);
		int8_t* L_190 = L_189;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_190, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)2), 8))))) = (double)(0.032011999999999999);
		int8_t* L_191 = L_190;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_191, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)3), 8))))) = (double)(0.014423);
		int8_t* L_192 = L_191;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_192, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)4), 8))))) = (double)(0.0071492999999999999);
		int8_t* L_193 = L_192;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_193, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)5), 8))))) = (double)(0.0040036999999999998);
		int8_t* L_194 = L_193;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_194, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)6), 8))))) = (double)(0.0025366);
		int8_t* L_195 = L_194;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_195, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)7), 8))))) = (double)(0.0017359999999999999);
		int8_t* L_196 = L_195;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_196, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)8), 8))))) = (double)(0.0012503);
		int8_t* L_197 = L_196;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_197, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)9)), 8))))) = (double)(0.00093568999999999998);
		int8_t* L_198 = L_197;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_198, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)10)), 8))))) = (double)(0.00072028999999999995);
		int8_t* L_199 = L_198;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_199, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)11)), 8))))) = (double)(0.00056689999999999996);
		int8_t* L_200 = L_199;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_200, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)12)), 8))))) = (double)(0.00045422000000000003);
		int8_t* L_201 = L_200;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_201, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)13)), 8))))) = (double)(0.00036928999999999998);
		int8_t* L_202 = L_201;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_202, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)14)), 8))))) = (double)(0.00030393999999999999);
		int8_t* L_203 = L_202;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_203, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)15)), 8))))) = (double)(0.00025276999999999998);
		int8_t* L_204 = L_203;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_204, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)16)), 8))))) = (double)(0.00021210000000000001);
		int8_t* L_205 = L_204;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_205, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)17)), 8))))) = (double)(0.00017935999999999999);
		int8_t* L_206 = L_205;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_206, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)18)), 8))))) = (double)(0.00015271999999999999);
		int8_t* L_207 = L_206;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_207, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)19)), 8))))) = (double)(0.00013081);
		int8_t* L_208 = L_207;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_208, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)20)), 8))))) = (double)(0.00011265);
		int8_t* L_209 = L_208;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_209, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)21)), 8))))) = (double)(9.7489000000000001E-05);
		int8_t* L_210 = L_209;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_210, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)22)), 8))))) = (double)(8.4735999999999996E-05);
		int8_t* L_211 = L_210;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_211, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)23)), 8))))) = (double)(7.3943000000000007E-05);
		int8_t* L_212 = L_211;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_212, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)24)), 8))))) = (double)(6.4757000000000004E-05);
		int8_t* L_213 = L_212;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_213, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)25)), 8))))) = (double)(5.6901999999999998E-05);
		int8_t* L_214 = L_213;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_214, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)26)), 8))))) = (double)(5.0149999999999999E-05);
		int8_t* L_215 = L_214;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_215, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)27)), 8))))) = (double)(4.4320999999999998E-05);
		int8_t* L_216 = L_215;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_216, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)28)), 8))))) = (double)(3.9270000000000002E-05);
		int8_t* L_217 = L_216;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_217, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)29)), 8))))) = (double)(3.4879000000000003E-05);
		int8_t* L_218 = L_217;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_218, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)30)), 8))))) = (double)(3.1050999999999998E-05);
		int8_t* L_219 = L_218;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_219, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)31)), 8))))) = (double)(2.7701000000000001E-05);
		int8_t* L_220 = L_219;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_220, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)32)), 8))))) = (double)(2.4762000000000001E-05);
		int8_t* L_221 = L_220;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_221, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)33)), 8))))) = (double)(2.2175999999999999E-05);
		int8_t* L_222 = L_221;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_222, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)34)), 8))))) = (double)(1.9894E-05);
		int8_t* L_223 = L_222;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_223, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)35)), 8))))) = (double)(1.7875000000000001E-05);
		int8_t* L_224 = L_223;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_224, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)36)), 8))))) = (double)(1.6084000000000001E-05);
		int8_t* L_225 = L_224;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_225, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)37)), 8))))) = (double)(1.4494E-05);
		int8_t* L_226 = L_225;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_226, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)38)), 8))))) = (double)(1.3078000000000001E-05);
		int8_t* L_227 = L_226;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_227, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)39)), 8))))) = (double)(1.1815E-05);
		int8_t* L_228 = L_227;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_228, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)40)), 8))))) = (double)(1.0686000000000001E-05);
		int8_t* L_229 = L_228;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_229, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)41)), 8))))) = (double)(9.6760000000000002E-06);
		int8_t* L_230 = L_229;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_230, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)42)), 8))))) = (double)(8.7703999999999999E-06);
		int8_t* L_231 = L_230;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_231, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)43)), 8))))) = (double)(7.9571000000000004E-06);
		int8_t* L_232 = L_231;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_232, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)44)), 8))))) = (double)(7.2258999999999997E-06);
		int8_t* L_233 = L_232;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_233, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)45)), 8))))) = (double)(6.5678E-06);
		int8_t* L_234 = L_233;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_234, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)46)), 8))))) = (double)(5.9748E-06);
		int8_t* L_235 = L_234;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_235, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)47)), 8))))) = (double)(5.4399999999999996E-06);
		int8_t* L_236 = L_235;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_236, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)48)), 8))))) = (double)(4.9571000000000004E-06);
		int8_t* L_237 = L_236;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_237, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)49)), 8))))) = (double)(4.5208E-06);
		int8_t* L_238 = L_237;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_238, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)50)), 8))))) = (double)(4.126E-06);
		int8_t* L_239 = L_238;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_239, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)51)), 8))))) = (double)(3.7685999999999998E-06);
		int8_t* L_240 = L_239;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_240, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)52)), 8))))) = (double)(3.4446E-06);
		int8_t* L_241 = L_240;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_241, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)53)), 8))))) = (double)(3.1507000000000002E-06);
		int8_t* L_242 = L_241;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_242, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)54)), 8))))) = (double)(2.8839E-06);
		int8_t* L_243 = L_242;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_243, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)55)), 8))))) = (double)(2.6413999999999998E-06);
		int8_t* L_244 = L_243;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_244, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)56)), 8))))) = (double)(2.4208000000000002E-06);
		int8_t* L_245 = L_244;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_245, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)57)), 8))))) = (double)(2.2199999999999999E-06);
		int8_t* L_246 = L_245;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_246, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)58)), 8))))) = (double)(2.0370000000000001E-06);
		int8_t* L_247 = L_246;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_247, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)59)), 8))))) = (double)(1.8701999999999999E-06);
		int8_t* L_248 = L_247;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_248, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)60)), 8))))) = (double)(1.7179000000000001E-06);
		int8_t* L_249 = L_248;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_249, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)61)), 8))))) = (double)(1.5789E-06);
		int8_t* L_250 = L_249;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_250, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)62)), 8))))) = (double)(1.4518E-06);
		int8_t* L_251 = L_250;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_251, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)63)), 8))))) = (double)(1.3356E-06);
		int8_t* L_252 = L_251;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_252, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)64)), 8))))) = (double)(1.2292E-06);
		int8_t* L_253 = L_252;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_253, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)65)), 8))))) = (double)(1.1318999999999999E-06);
		int8_t* L_254 = L_253;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_254, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)66)), 8))))) = (double)(1.0428E-06);
		int8_t* L_255 = L_254;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_255, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)67)), 8))))) = (double)(9.6118999999999994E-07);
		int8_t* L_256 = L_255;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_256, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)68)), 8))))) = (double)(8.8642000000000002E-07);
		int8_t* L_257 = L_256;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_257, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)69)), 8))))) = (double)(8.1788999999999997E-07);
		int8_t* L_258 = L_257;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_258, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)70)), 8))))) = (double)(7.5504999999999997E-07);
		int8_t* L_259 = L_258;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_259, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)71)), 8))))) = (double)(6.9741000000000005E-07);
		int8_t* L_260 = L_259;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_260, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)72)), 8))))) = (double)(6.4451999999999995E-07);
		int8_t* L_261 = L_260;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_261, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)73)), 8))))) = (double)(5.9597000000000001E-07);
		int8_t* L_262 = L_261;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_262, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)74)), 8))))) = (double)(5.5138999999999996E-07);
		int8_t* L_263 = L_262;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_263, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)75)), 8))))) = (double)(5.1043E-07);
		int8_t* L_264 = L_263;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_264, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)76)), 8))))) = (double)(4.7278999999999999E-07);
		int8_t* L_265 = L_264;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_265, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)77)), 8))))) = (double)(4.3818000000000002E-07);
		int8_t* L_266 = L_265;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_266, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)78)), 8))))) = (double)(4.0634000000000002E-07);
		int8_t* L_267 = L_266;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_267, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)79)), 8))))) = (double)(3.7705E-07);
		int8_t* L_268 = L_267;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_268, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)80)), 8))))) = (double)(3.5008000000000002E-07);
		int8_t* L_269 = L_268;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_269, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)81)), 8))))) = (double)(3.2524000000000002E-07);
		int8_t* L_270 = L_269;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_270, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)82)), 8))))) = (double)(3.0235E-07);
		int8_t* L_271 = L_270;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_271, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)83)), 8))))) = (double)(2.8126000000000002E-07);
		int8_t* L_272 = L_271;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_272, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)84)), 8))))) = (double)(2.6179999999999998E-07);
		int8_t* L_273 = L_272;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_273, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)85)), 8))))) = (double)(2.4386000000000001E-07);
		int8_t* L_274 = L_273;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_274, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)86)), 8))))) = (double)(2.273E-07);
		int8_t* L_275 = L_274;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_275, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)87)), 8))))) = (double)(2.1201999999999999E-07);
		int8_t* L_276 = L_275;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_276, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)88)), 8))))) = (double)(1.9789999999999999E-07);
		int8_t* L_277 = L_276;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_277, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)89)), 8))))) = (double)(1.8486E-07);
		int8_t* L_278 = L_277;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_278, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)90)), 8))))) = (double)(1.7281E-07);
		int8_t* L_279 = L_278;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_279, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)91)), 8))))) = (double)(1.6168E-07);
		int8_t* L_280 = L_279;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_280, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)92)), 8))))) = (double)(1.5137000000000001E-07);
		int8_t* L_281 = L_280;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_281, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)93)), 8))))) = (double)(1.4184E-07);
		int8_t* L_282 = L_281;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_282, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)94)), 8))))) = (double)(1.3302000000000001E-07);
		int8_t* L_283 = L_282;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_283, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)95)), 8))))) = (double)(1.2485000000000001E-07);
		int8_t* L_284 = L_283;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_284, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)96)), 8))))) = (double)(1.1728E-07);
		int8_t* L_285 = L_284;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_285, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)97)), 8))))) = (double)(1.1026999999999999E-07);
		int8_t* L_286 = L_285;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_286, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)98)), 8))))) = (double)(1.0377E-07);
		int8_t* L_287 = L_286;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_287, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)99)), 8))))) = (double)(9.7738999999999999E-08);
		int8_t* L_288 = L_287;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_288, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)100)), 8))))) = (double)(9.2144999999999996E-08);
		int8_t* L_289 = L_288;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_289, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)101)), 8))))) = (double)(8.6951999999999997E-08);
		int8_t* L_290 = L_289;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_290, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)102)), 8))))) = (double)(8.2129999999999995E-08);
		int8_t* L_291 = L_290;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_291, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)103)), 8))))) = (double)(7.7649999999999997E-08);
		int8_t* L_292 = L_291;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_292, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)104)), 8))))) = (double)(7.3486E-08);
		int8_t* L_293 = L_292;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_293, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)105)), 8))))) = (double)(6.9612000000000002E-08);
		int8_t* L_294 = L_293;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_294, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)106)), 8))))) = (double)(6.6005000000000001E-08);
		int8_t* L_295 = L_294;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_295, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)107)), 8))))) = (double)(6.2644000000000002E-08);
		int8_t* L_296 = L_295;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_296, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)108)), 8))))) = (double)(5.9510000000000001E-08);
		int8_t* L_297 = L_296;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_297, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)109)), 8))))) = (double)(5.6585E-08);
		int8_t* L_298 = L_297;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_298, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)110)), 8))))) = (double)(5.3855000000000001E-08);
		int8_t* L_299 = L_298;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_299, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)111)), 8))))) = (double)(5.1303E-08);
		int8_t* L_300 = L_299;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_300, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)112)), 8))))) = (double)(4.8917000000000001E-08);
		int8_t* L_301 = L_300;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_301, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)113)), 8))))) = (double)(4.6684000000000003E-08);
		int8_t* L_302 = L_301;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_302, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)114)), 8))))) = (double)(4.4594000000000002E-08);
		int8_t* L_303 = L_302;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_303, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)115)), 8))))) = (double)(4.2634999999999998E-08);
		int8_t* L_304 = L_303;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_304, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)116)), 8))))) = (double)(4.0799E-08);
		int8_t* L_305 = L_304;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_305, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)117)), 8))))) = (double)(3.9075999999999999E-08);
		int8_t* L_306 = L_305;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_306, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)118)), 8))))) = (double)(3.7458999999999998E-08);
		int8_t* L_307 = L_306;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_307, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)119)), 8))))) = (double)(3.5940000000000003E-08);
		int8_t* L_308 = L_307;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_308, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)120)), 8))))) = (double)(3.4512999999999999E-08);
		int8_t* L_309 = L_308;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_309, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)121)), 8))))) = (double)(3.3171E-08);
		int8_t* L_310 = L_309;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_310, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)122)), 8))))) = (double)(3.1907999999999997E-08);
		int8_t* L_311 = L_310;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_311, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)123)), 8))))) = (double)(3.072E-08);
		int8_t* L_312 = L_311;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_312, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)124)), 8))))) = (double)(2.96E-08);
		int8_t* L_313 = L_312;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_313, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)125)), 8))))) = (double)(2.8543E-08);
		int8_t* L_314 = L_313;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_314, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)126)), 8))))) = (double)(2.7546000000000001E-08);
		int8_t* L_315 = L_314;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_315, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)127)), 8))))) = (double)(2.6604999999999999E-08);
		int8_t* L_316 = L_315;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_316, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)128)), 8))))) = (double)(2.5714000000000001E-08);
		int8_t* L_317 = L_316;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_317, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)129)), 8))))) = (double)(2.4871000000000001E-08);
		int8_t* L_318 = L_317;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_318, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)130)), 8))))) = (double)(2.4071999999999999E-08);
		int8_t* L_319 = L_318;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_319, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)131)), 8))))) = (double)(2.3314E-08);
		int8_t* L_320 = L_319;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_320, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)132)), 8))))) = (double)(2.2595000000000001E-08);
		int8_t* L_321 = L_320;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_321, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)133)), 8))))) = (double)(2.1912000000000001E-08);
		int8_t* L_322 = L_321;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_322, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)134)), 8))))) = (double)(2.1263E-08);
		int8_t* L_323 = L_322;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_323, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)135)), 8))))) = (double)(2.0643999999999999E-08);
		int8_t* L_324 = L_323;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_324, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)136)), 8))))) = (double)(2.0055E-08);
		int8_t* L_325 = L_324;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_325, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)137)), 8))))) = (double)(1.9493000000000001E-08);
		int8_t* L_326 = L_325;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_326, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)138)), 8))))) = (double)(1.8956999999999999E-08);
		int8_t* L_327 = L_326;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_327, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)139)), 8))))) = (double)(1.8445000000000001E-08);
		int8_t* L_328 = L_327;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_328, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)140)), 8))))) = (double)(1.7955000000000001E-08);
		int8_t* L_329 = L_328;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_329, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)141)), 8))))) = (double)(1.7485000000000001E-08);
		int8_t* L_330 = L_329;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_330, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)142)), 8))))) = (double)(1.7036000000000001E-08);
		int8_t* L_331 = L_330;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_331, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)143)), 8))))) = (double)(1.6604000000000001E-08);
		int8_t* L_332 = L_331;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_332, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)144)), 8))))) = (double)(1.6190000000000001E-08);
		int8_t* L_333 = L_332;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_333, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)145)), 8))))) = (double)(1.5793000000000001E-08);
		int8_t* L_334 = L_333;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_334, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)146)), 8))))) = (double)(1.5411000000000001E-08);
		int8_t* L_335 = L_334;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_335, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)147)), 8))))) = (double)(1.5043999999999999E-08);
		int8_t* L_336 = L_335;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_336, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)148)), 8))))) = (double)(1.469E-08);
		int8_t* L_337 = L_336;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_337, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)149)), 8))))) = (double)(1.4349999999999999E-08);
		int8_t* L_338 = L_337;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_338, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)150)), 8))))) = (double)(1.4022999999999999E-08);
		int8_t* L_339 = L_338;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_339, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)151)), 8))))) = (double)(1.3707E-08);
		int8_t* L_340 = L_339;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_340, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)152)), 8))))) = (double)(1.3402999999999999E-08);
		int8_t* L_341 = L_340;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_341, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)153)), 8))))) = (double)(1.311E-08);
		int8_t* L_342 = L_341;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_342, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)154)), 8))))) = (double)(1.2827E-08);
		int8_t* L_343 = L_342;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_343, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)155)), 8))))) = (double)(1.2553E-08);
		int8_t* L_344 = L_343;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_344, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)156)), 8))))) = (double)(1.2289E-08);
		int8_t* L_345 = L_344;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_345, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)157)), 8))))) = (double)(1.2032999999999999E-08);
		int8_t* L_346 = L_345;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_346, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)158)), 8))))) = (double)(1.1786E-08);
		int8_t* L_347 = L_346;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_347, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)159)), 8))))) = (double)(1.1547000000000001E-08);
		int8_t* L_348 = L_347;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_348, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)160)), 8))))) = (double)(1.1315000000000001E-08);
		int8_t* L_349 = L_348;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_349, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)161)), 8))))) = (double)(1.1091E-08);
		int8_t* L_350 = L_349;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_350, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)162)), 8))))) = (double)(1.0873000000000001E-08);
		int8_t* L_351 = L_350;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_351, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)163)), 8))))) = (double)(1.0662E-08);
		int8_t* L_352 = L_351;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_352, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)164)), 8))))) = (double)(1.0457000000000001E-08);
		int8_t* L_353 = L_352;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_353, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)165)), 8))))) = (double)(1.0258E-08);
		int8_t* L_354 = L_353;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_354, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)166)), 8))))) = (double)(1.0064E-08);
		int8_t* L_355 = L_354;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_355, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)167)), 8))))) = (double)(9.8752E-09);
		int8_t* L_356 = L_355;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_356, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)168)), 8))))) = (double)(9.6914999999999995E-09);
		int8_t* L_357 = L_356;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_357, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)169)), 8))))) = (double)(9.5126000000000004E-09);
		int8_t* L_358 = L_357;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_358, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)170)), 8))))) = (double)(9.3380000000000006E-09);
		int8_t* L_359 = L_358;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_359, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)171)), 8))))) = (double)(9.1677000000000001E-09);
		int8_t* L_360 = L_359;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_360, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)172)), 8))))) = (double)(9.0013000000000003E-09);
		int8_t* L_361 = L_360;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_361, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)173)), 8))))) = (double)(8.8386999999999993E-09);
		int8_t* L_362 = L_361;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_362, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)174)), 8))))) = (double)(8.6796000000000005E-09);
		int8_t* L_363 = L_362;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_363, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)175)), 8))))) = (double)(8.5239000000000003E-09);
		int8_t* L_364 = L_363;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_364, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)176)), 8))))) = (double)(8.3713000000000007E-09);
		int8_t* L_365 = L_364;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_365, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)177)), 8))))) = (double)(8.2217999999999999E-09);
		int8_t* L_366 = L_365;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_366, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)178)), 8))))) = (double)(8.0751E-09);
		int8_t* L_367 = L_366;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_367, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)179)), 8))))) = (double)(7.9311000000000005E-09);
		int8_t* L_368 = L_367;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_368, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)180)), 8))))) = (double)(7.7896000000000005E-09);
		int8_t* L_369 = L_368;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_369, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)181)), 8))))) = (double)(7.6504999999999996E-09);
		int8_t* L_370 = L_369;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_370, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)182)), 8))))) = (double)(7.5137999999999994E-09);
		Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5 L_371;
		memset((&L_371), 0, sizeof(L_371));
		Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_inline((&L_371), (void*)L_370, ((int32_t)183), Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_RuntimeMethod_var);
		V_1 = L_371;
		uintptr_t L_372 = ((uintptr_t)((int32_t)1464));
		int8_t* L_373 = (int8_t*) (L_372 ? alloca(L_372) : NULL);
		memset(L_373, 0, L_372);
		int8_t* L_374 = (L_373);
		*((double*)L_374) = (double)(3.4400000000000001E-06);
		int8_t* L_375 = L_374;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_375, 8))) = (double)(1.3873E-06);
		int8_t* L_376 = L_375;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_376, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)2), 8))))) = (double)(5.6044000000000001E-07);
		int8_t* L_377 = L_376;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_377, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)3), 8))))) = (double)(2.3325E-07);
		int8_t* L_378 = L_377;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_378, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)4), 8))))) = (double)(9.6733999999999998E-08);
		int8_t* L_379 = L_378;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_379, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)5), 8))))) = (double)(4.2793999999999999E-08);
		int8_t* L_380 = L_379;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_380, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)6), 8))))) = (double)(2.2198999999999999E-08);
		int8_t* L_381 = L_380;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_381, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)7), 8))))) = (double)(1.2918E-08);
		int8_t* L_382 = L_381;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_382, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)8), 8))))) = (double)(8.1493999999999992E-09);
		int8_t* L_383 = L_382;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_383, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)9)), 8))))) = (double)(5.4647E-09);
		int8_t* L_384 = L_383;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_384, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)10)), 8))))) = (double)(3.8313000000000002E-09);
		int8_t* L_385 = L_384;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_385, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)11)), 8))))) = (double)(2.7805000000000001E-09);
		int8_t* L_386 = L_385;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_386, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)12)), 8))))) = (double)(2.0752E-09);
		int8_t* L_387 = L_386;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_387, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)13)), 8))))) = (double)(1.5847999999999999E-09);
		int8_t* L_388 = L_387;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_388, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)14)), 8))))) = (double)(1.2336E-09);
		int8_t* L_389 = L_388;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_389, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)15)), 8))))) = (double)(9.7526E-10);
		int8_t* L_390 = L_389;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_390, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)16)), 8))))) = (double)(7.8154999999999995E-10);
		int8_t* L_391 = L_390;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_391, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)17)), 8))))) = (double)(6.3381999999999999E-10);
		int8_t* L_392 = L_391;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_392, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)18)), 8))))) = (double)(5.1939999999999998E-10);
		int8_t* L_393 = L_392;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_393, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)19)), 8))))) = (double)(4.2951999999999998E-10);
		int8_t* L_394 = L_393;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_394, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)20)), 8))))) = (double)(3.5806999999999998E-10);
		int8_t* L_395 = L_394;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_395, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)21)), 8))))) = (double)(3.0064000000000001E-10);
		int8_t* L_396 = L_395;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_396, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)22)), 8))))) = (double)(2.5406999999999998E-10);
		int8_t* L_397 = L_396;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_397, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)23)), 8))))) = (double)(2.1596E-10);
		int8_t* L_398 = L_397;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_398, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)24)), 8))))) = (double)(1.8456000000000001E-10);
		int8_t* L_399 = L_398;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_399, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)25)), 8))))) = (double)(1.5848999999999999E-10);
		int8_t* L_400 = L_399;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_400, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)26)), 8))))) = (double)(1.3671000000000001E-10);
		int8_t* L_401 = L_400;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_401, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)27)), 8))))) = (double)(1.1838999999999999E-10);
		int8_t* L_402 = L_401;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_402, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)28)), 8))))) = (double)(1.029E-10);
		int8_t* L_403 = L_402;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_403, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)29)), 8))))) = (double)(8.9756999999999996E-11);
		int8_t* L_404 = L_403;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_404, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)30)), 8))))) = (double)(7.8550000000000003E-11);
		int8_t* L_405 = L_404;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_405, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)31)), 8))))) = (double)(6.8954E-11);
		int8_t* L_406 = L_405;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_406, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)32)), 8))))) = (double)(6.0705999999999994E-11);
		int8_t* L_407 = L_406;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_407, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)33)), 8))))) = (double)(5.3586999999999999E-11);
		int8_t* L_408 = L_407;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_408, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)34)), 8))))) = (double)(4.7420000000000003E-11);
		int8_t* L_409 = L_408;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_409, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)35)), 8))))) = (double)(4.2057999999999997E-11);
		int8_t* L_410 = L_409;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_410, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)36)), 8))))) = (double)(3.7382000000000002E-11);
		int8_t* L_411 = L_410;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_411, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)37)), 8))))) = (double)(3.3293999999999998E-11);
		int8_t* L_412 = L_411;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_412, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)38)), 8))))) = (double)(2.971E-11);
		int8_t* L_413 = L_412;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_413, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)39)), 8))))) = (double)(2.656E-11);
		int8_t* L_414 = L_413;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_414, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)40)), 8))))) = (double)(2.3783000000000001E-11);
		int8_t* L_415 = L_414;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_415, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)41)), 8))))) = (double)(2.1331000000000001E-11);
		int8_t* L_416 = L_415;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_416, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)42)), 8))))) = (double)(1.9158999999999999E-11);
		int8_t* L_417 = L_416;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_417, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)43)), 8))))) = (double)(1.7231999999999999E-11);
		int8_t* L_418 = L_417;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_418, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)44)), 8))))) = (double)(1.5519E-11);
		int8_t* L_419 = L_418;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_419, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)45)), 8))))) = (double)(1.3994E-11);
		int8_t* L_420 = L_419;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_420, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)46)), 8))))) = (double)(1.2634E-11);
		int8_t* L_421 = L_420;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_421, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)47)), 8))))) = (double)(1.1419000000000001E-11);
		int8_t* L_422 = L_421;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_422, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)48)), 8))))) = (double)(1.0333E-11);
		int8_t* L_423 = L_422;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_423, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)49)), 8))))) = (double)(9.3607000000000007E-12);
		int8_t* L_424 = L_423;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_424, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)50)), 8))))) = (double)(8.4886000000000008E-12);
		int8_t* L_425 = L_424;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_425, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)51)), 8))))) = (double)(7.7054000000000007E-12);
		int8_t* L_426 = L_425;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_426, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)52)), 8))))) = (double)(7.0011000000000001E-12);
		int8_t* L_427 = L_426;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_427, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)53)), 8))))) = (double)(6.3669999999999998E-12);
		int8_t* L_428 = L_427;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_428, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)54)), 8))))) = (double)(5.7954000000000001E-12);
		int8_t* L_429 = L_428;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_429, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)55)), 8))))) = (double)(5.2794999999999999E-12);
		int8_t* L_430 = L_429;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_430, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)56)), 8))))) = (double)(4.8131999999999997E-12);
		int8_t* L_431 = L_430;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_431, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)57)), 8))))) = (double)(4.3914E-12);
		int8_t* L_432 = L_431;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_432, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)58)), 8))))) = (double)(4.0092999999999999E-12);
		int8_t* L_433 = L_432;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_433, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)59)), 8))))) = (double)(3.6628999999999997E-12);
		int8_t* L_434 = L_433;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_434, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)60)), 8))))) = (double)(3.3484000000000002E-12);
		int8_t* L_435 = L_434;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_435, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)61)), 8))))) = (double)(3.0626999999999999E-12);
		int8_t* L_436 = L_435;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_436, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)62)), 8))))) = (double)(2.8028000000000001E-12);
		int8_t* L_437 = L_436;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_437, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)63)), 8))))) = (double)(2.5662000000000001E-12);
		int8_t* L_438 = L_437;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_438, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)64)), 8))))) = (double)(2.3507000000000001E-12);
		int8_t* L_439 = L_438;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_439, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)65)), 8))))) = (double)(2.1542999999999998E-12);
		int8_t* L_440 = L_439;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_440, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)66)), 8))))) = (double)(1.9751999999999999E-12);
		int8_t* L_441 = L_440;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_441, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)67)), 8))))) = (double)(1.8118E-12);
		int8_t* L_442 = L_441;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_442, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)68)), 8))))) = (double)(1.6626E-12);
		int8_t* L_443 = L_442;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_443, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)69)), 8))))) = (double)(1.5265E-12);
		int8_t* L_444 = L_443;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_444, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)70)), 8))))) = (double)(1.4020000000000001E-12);
		int8_t* L_445 = L_444;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_445, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)71)), 8))))) = (double)(1.2883000000000001E-12);
		int8_t* L_446 = L_445;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_446, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)72)), 8))))) = (double)(1.1842999999999999E-12);
		int8_t* L_447 = L_446;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_447, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)73)), 8))))) = (double)(1.0891000000000001E-12);
		int8_t* L_448 = L_447;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_448, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)74)), 8))))) = (double)(1.002E-12);
		int8_t* L_449 = L_448;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_449, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)75)), 8))))) = (double)(9.2222000000000008E-13);
		int8_t* L_450 = L_449;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_450, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)76)), 8))))) = (double)(8.4913000000000004E-13);
		int8_t* L_451 = L_450;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_451, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)77)), 8))))) = (double)(7.8214000000000004E-13);
		int8_t* L_452 = L_451;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_452, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)78)), 8))))) = (double)(7.2070000000000005E-13);
		int8_t* L_453 = L_452;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_453, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)79)), 8))))) = (double)(6.6433999999999997E-13);
		int8_t* L_454 = L_453;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_454, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)80)), 8))))) = (double)(6.1260999999999995E-13);
		int8_t* L_455 = L_454;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_455, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)81)), 8))))) = (double)(5.6511E-13);
		int8_t* L_456 = L_455;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_456, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)82)), 8))))) = (double)(5.2148000000000002E-13);
		int8_t* L_457 = L_456;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_457, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)83)), 8))))) = (double)(4.8139000000000003E-13);
		int8_t* L_458 = L_457;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_458, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)84)), 8))))) = (double)(4.4453999999999999E-13);
		int8_t* L_459 = L_458;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_459, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)85)), 8))))) = (double)(4.1065000000000002E-13);
		int8_t* L_460 = L_459;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_460, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)86)), 8))))) = (double)(3.7948999999999998E-13);
		int8_t* L_461 = L_460;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_461, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)87)), 8))))) = (double)(3.5082999999999998E-13);
		int8_t* L_462 = L_461;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_462, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)88)), 8))))) = (double)(3.2446000000000001E-13);
		int8_t* L_463 = L_462;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_463, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)89)), 8))))) = (double)(3.0019000000000001E-13);
		int8_t* L_464 = L_463;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_464, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)90)), 8))))) = (double)(2.7785000000000002E-13);
		int8_t* L_465 = L_464;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_465, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)91)), 8))))) = (double)(2.5727000000000002E-13);
		int8_t* L_466 = L_465;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_466, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)92)), 8))))) = (double)(2.3831999999999999E-13);
		int8_t* L_467 = L_466;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_467, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)93)), 8))))) = (double)(2.2086000000000001E-13);
		int8_t* L_468 = L_467;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_468, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)94)), 8))))) = (double)(2.0476999999999999E-13);
		int8_t* L_469 = L_468;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_469, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)95)), 8))))) = (double)(1.8993E-13);
		int8_t* L_470 = L_469;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_470, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)96)), 8))))) = (double)(1.7625000000000001E-13);
		int8_t* L_471 = L_470;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_471, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)97)), 8))))) = (double)(1.6363000000000001E-13);
		int8_t* L_472 = L_471;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_472, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)98)), 8))))) = (double)(1.5198999999999999E-13);
		int8_t* L_473 = L_472;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_473, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)99)), 8))))) = (double)(1.4124000000000001E-13);
		int8_t* L_474 = L_473;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_474, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)100)), 8))))) = (double)(1.3131E-13);
		int8_t* L_475 = L_474;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_475, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)101)), 8))))) = (double)(1.2214E-13);
		int8_t* L_476 = L_475;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_476, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)102)), 8))))) = (double)(1.1367E-13);
		int8_t* L_477 = L_476;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_477, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)103)), 8))))) = (double)(1.0583999999999999E-13);
		int8_t* L_478 = L_477;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_478, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)104)), 8))))) = (double)(9.8597000000000001E-14);
		int8_t* L_479 = L_478;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_479, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)105)), 8))))) = (double)(9.1902999999999995E-14);
		int8_t* L_480 = L_479;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_480, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)106)), 8))))) = (double)(8.5709999999999995E-14);
		int8_t* L_481 = L_480;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_481, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)107)), 8))))) = (double)(7.9981E-14);
		int8_t* L_482 = L_481;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_482, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)108)), 8))))) = (double)(7.4677999999999997E-14);
		int8_t* L_483 = L_482;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_483, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)109)), 8))))) = (double)(6.9770000000000002E-14);
		int8_t* L_484 = L_483;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_484, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)110)), 8))))) = (double)(6.5224999999999995E-14);
		int8_t* L_485 = L_484;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_485, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)111)), 8))))) = (double)(6.1015000000000006E-14);
		int8_t* L_486 = L_485;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_486, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)112)), 8))))) = (double)(5.7113999999999995E-14);
		int8_t* L_487 = L_486;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_487, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)113)), 8))))) = (double)(5.3499000000000003E-14);
		int8_t* L_488 = L_487;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_488, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)114)), 8))))) = (double)(5.0146999999999997E-14);
		int8_t* L_489 = L_488;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_489, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)115)), 8))))) = (double)(4.7038000000000001E-14);
		int8_t* L_490 = L_489;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_490, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)116)), 8))))) = (double)(4.4153999999999999E-14);
		int8_t* L_491 = L_490;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_491, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)117)), 8))))) = (double)(4.1477999999999998E-14);
		int8_t* L_492 = L_491;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_492, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)118)), 8))))) = (double)(3.8993000000000002E-14);
		int8_t* L_493 = L_492;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_493, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)119)), 8))))) = (double)(3.6686E-14);
		int8_t* L_494 = L_493;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_494, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)120)), 8))))) = (double)(3.4541999999999997E-14);
		int8_t* L_495 = L_494;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_495, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)121)), 8))))) = (double)(3.2549999999999998E-14);
		int8_t* L_496 = L_495;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_496, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)122)), 8))))) = (double)(3.0697999999999998E-14);
		int8_t* L_497 = L_496;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_497, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)123)), 8))))) = (double)(2.8976000000000001E-14);
		int8_t* L_498 = L_497;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_498, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)124)), 8))))) = (double)(2.7374E-14);
		int8_t* L_499 = L_498;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_499, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)125)), 8))))) = (double)(2.5881999999999999E-14);
		int8_t* L_500 = L_499;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_500, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)126)), 8))))) = (double)(2.4491000000000001E-14);
		int8_t* L_501 = L_500;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_501, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)127)), 8))))) = (double)(2.3195999999999999E-14);
		int8_t* L_502 = L_501;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_502, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)128)), 8))))) = (double)(2.1987E-14);
		int8_t* L_503 = L_502;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_503, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)129)), 8))))) = (double)(2.0857999999999999E-14);
		int8_t* L_504 = L_503;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_504, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)130)), 8))))) = (double)(1.9805000000000001E-14);
		int8_t* L_505 = L_504;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_505, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)131)), 8))))) = (double)(1.8819999999999999E-14);
		int8_t* L_506 = L_505;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_506, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)132)), 8))))) = (double)(1.7900000000000001E-14);
		int8_t* L_507 = L_506;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_507, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)133)), 8))))) = (double)(1.7038999999999999E-14);
		int8_t* L_508 = L_507;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_508, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)134)), 8))))) = (double)(1.6233E-14);
		int8_t* L_509 = L_508;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_509, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)135)), 8))))) = (double)(1.5478000000000001E-14);
		int8_t* L_510 = L_509;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_510, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)136)), 8))))) = (double)(1.4771E-14);
		int8_t* L_511 = L_510;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_511, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)137)), 8))))) = (double)(1.4108E-14);
		int8_t* L_512 = L_511;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_512, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)138)), 8))))) = (double)(1.3487E-14);
		int8_t* L_513 = L_512;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_513, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)139)), 8))))) = (double)(1.2903E-14);
		int8_t* L_514 = L_513;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_514, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)140)), 8))))) = (double)(1.2356E-14);
		int8_t* L_515 = L_514;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_515, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)141)), 8))))) = (double)(1.1841E-14);
		int8_t* L_516 = L_515;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_516, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)142)), 8))))) = (double)(1.1358E-14);
		int8_t* L_517 = L_516;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_517, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)143)), 8))))) = (double)(1.0904E-14);
		int8_t* L_518 = L_517;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_518, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)144)), 8))))) = (double)(1.0477E-14);
		int8_t* L_519 = L_518;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_519, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)145)), 8))))) = (double)(1.0073999999999999E-14);
		int8_t* L_520 = L_519;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_520, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)146)), 8))))) = (double)(9.6947000000000003E-15);
		int8_t* L_521 = L_520;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_521, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)147)), 8))))) = (double)(9.3370000000000001E-15);
		int8_t* L_522 = L_521;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_522, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)148)), 8))))) = (double)(8.9992E-15);
		int8_t* L_523 = L_522;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_523, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)149)), 8))))) = (double)(8.6801E-15);
		int8_t* L_524 = L_523;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_524, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)150)), 8))))) = (double)(8.3782999999999997E-15);
		int8_t* L_525 = L_524;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_525, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)151)), 8))))) = (double)(8.0927999999999993E-15);
		int8_t* L_526 = L_525;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_526, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)152)), 8))))) = (double)(7.8222999999999993E-15);
		int8_t* L_527 = L_526;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_527, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)153)), 8))))) = (double)(7.5658999999999993E-15);
		int8_t* L_528 = L_527;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_528, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)154)), 8))))) = (double)(7.3227000000000004E-15);
		int8_t* L_529 = L_528;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_529, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)155)), 8))))) = (double)(7.0916999999999996E-15);
		int8_t* L_530 = L_529;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_530, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)156)), 8))))) = (double)(6.8723000000000001E-15);
		int8_t* L_531 = L_530;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_531, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)157)), 8))))) = (double)(6.6635000000000003E-15);
		int8_t* L_532 = L_531;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_532, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)158)), 8))))) = (double)(6.4648999999999998E-15);
		int8_t* L_533 = L_532;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_533, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)159)), 8))))) = (double)(6.2756999999999998E-15);
		int8_t* L_534 = L_533;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_534, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)160)), 8))))) = (double)(6.0952000000000001E-15);
		int8_t* L_535 = L_534;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_535, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)161)), 8))))) = (double)(5.9231000000000003E-15);
		int8_t* L_536 = L_535;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_536, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)162)), 8))))) = (double)(5.7587000000000003E-15);
		int8_t* L_537 = L_536;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_537, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)163)), 8))))) = (double)(5.6016000000000001E-15);
		int8_t* L_538 = L_537;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_538, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)164)), 8))))) = (double)(5.4514000000000002E-15);
		int8_t* L_539 = L_538;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_539, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)165)), 8))))) = (double)(5.3075000000000004E-15);
		int8_t* L_540 = L_539;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_540, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)166)), 8))))) = (double)(5.1697999999999997E-15);
		int8_t* L_541 = L_540;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_541, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)167)), 8))))) = (double)(5.0376999999999998E-15);
		int8_t* L_542 = L_541;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_542, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)168)), 8))))) = (double)(4.9110999999999997E-15);
		int8_t* L_543 = L_542;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_543, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)169)), 8))))) = (double)(4.7895000000000002E-15);
		int8_t* L_544 = L_543;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_544, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)170)), 8))))) = (double)(4.6727999999999997E-15);
		int8_t* L_545 = L_544;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_545, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)171)), 8))))) = (double)(4.5604999999999999E-15);
		int8_t* L_546 = L_545;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_546, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)172)), 8))))) = (double)(4.4524999999999997E-15);
		int8_t* L_547 = L_546;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_547, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)173)), 8))))) = (double)(4.3485999999999997E-15);
		int8_t* L_548 = L_547;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_548, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)174)), 8))))) = (double)(4.2484000000000002E-15);
		int8_t* L_549 = L_548;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_549, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)175)), 8))))) = (double)(4.1519000000000003E-15);
		int8_t* L_550 = L_549;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_550, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)176)), 8))))) = (double)(4.0587000000000003E-15);
		int8_t* L_551 = L_550;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_551, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)177)), 8))))) = (double)(3.9687E-15);
		int8_t* L_552 = L_551;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_552, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)178)), 8))))) = (double)(3.8818000000000001E-15);
		int8_t* L_553 = L_552;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_553, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)179)), 8))))) = (double)(3.7977000000000002E-15);
		int8_t* L_554 = L_553;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_554, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)180)), 8))))) = (double)(3.7163000000000001E-15);
		int8_t* L_555 = L_554;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_555, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)181)), 8))))) = (double)(3.6374999999999996E-15);
		int8_t* L_556 = L_555;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_556, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)182)), 8))))) = (double)(3.5611000000000002E-15);
		Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5 L_557;
		memset((&L_557), 0, sizeof(L_557));
		Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_inline((&L_557), (void*)L_556, ((int32_t)183), Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_RuntimeMethod_var);
		V_2 = L_557;
		uintptr_t L_558 = ((uintptr_t)((int32_t)1464));
		int8_t* L_559 = (int8_t*) (L_558 ? alloca(L_558) : NULL);
		memset(L_559, 0, L_558);
		int8_t* L_560 = (L_559);
		*((double*)L_560) = (double)(274.04000000000002);
		int8_t* L_561 = L_560;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_561, 8))) = (double)(275.17000000000002);
		int8_t* L_562 = L_561;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_562, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)2), 8))))) = (double)(280.0);
		int8_t* L_563 = L_562;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_563, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)3), 8))))) = (double)(289.69999999999999);
		int8_t* L_564 = L_563;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_564, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)4), 8))))) = (double)(310.56);
		int8_t* L_565 = L_564;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_565, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)5), 8))))) = (double)(347.22000000000003);
		int8_t* L_566 = L_565;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_566, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)6), 8))))) = (double)(380.36000000000001);
		int8_t* L_567 = L_566;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_567, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)7), 8))))) = (double)(409.48000000000002);
		int8_t* L_568 = L_567;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_568, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)8), 8))))) = (double)(434.26999999999998);
		int8_t* L_569 = L_568;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_569, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)9)), 8))))) = (double)(455.63999999999999);
		int8_t* L_570 = L_569;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_570, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)10)), 8))))) = (double)(474.24000000000001);
		int8_t* L_571 = L_570;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_571, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)11)), 8))))) = (double)(490.54000000000002);
		int8_t* L_572 = L_571;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_572, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)12)), 8))))) = (double)(504.92000000000002);
		int8_t* L_573 = L_572;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_573, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)13)), 8))))) = (double)(517.65999999999997);
		int8_t* L_574 = L_573;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_574, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)14)), 8))))) = (double)(528.98000000000002);
		int8_t* L_575 = L_574;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_575, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)15)), 8))))) = (double)(539.08000000000004);
		int8_t* L_576 = L_575;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_576, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)16)), 8))))) = (double)(548.11000000000001);
		int8_t* L_577 = L_576;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_577, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)17)), 8))))) = (double)(556.21000000000004);
		int8_t* L_578 = L_577;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_578, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)18)), 8))))) = (double)(563.48000000000002);
		int8_t* L_579 = L_578;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_579, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)19)), 8))))) = (double)(570.01999999999998);
		int8_t* L_580 = L_579;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_580, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)20)), 8))))) = (double)(575.90999999999997);
		int8_t* L_581 = L_580;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_581, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)21)), 8))))) = (double)(581.22000000000003);
		int8_t* L_582 = L_581;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_582, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)22)), 8))))) = (double)(586.01999999999998);
		int8_t* L_583 = L_582;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_583, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)23)), 8))))) = (double)(590.36000000000001);
		int8_t* L_584 = L_583;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_584, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)24)), 8))))) = (double)(594.28999999999996);
		int8_t* L_585 = L_584;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_585, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)25)), 8))))) = (double)(597.85000000000002);
		int8_t* L_586 = L_585;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_586, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)26)), 8))))) = (double)(601.07000000000005);
		int8_t* L_587 = L_586;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_587, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)27)), 8))))) = (double)(604.0);
		int8_t* L_588 = L_587;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_588, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)28)), 8))))) = (double)(606.64999999999998);
		int8_t* L_589 = L_588;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_589, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)29)), 8))))) = (double)(609.05999999999995);
		int8_t* L_590 = L_589;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_590, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)30)), 8))))) = (double)(611.25999999999999);
		int8_t* L_591 = L_590;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_591, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)31)), 8))))) = (double)(613.25);
		int8_t* L_592 = L_591;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_592, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)32)), 8))))) = (double)(615.05999999999995);
		int8_t* L_593 = L_592;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_593, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)33)), 8))))) = (double)(616.71000000000004);
		int8_t* L_594 = L_593;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_594, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)34)), 8))))) = (double)(618.21000000000004);
		int8_t* L_595 = L_594;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_595, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)35)), 8))))) = (double)(619.57000000000005);
		int8_t* L_596 = L_595;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_596, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)36)), 8))))) = (double)(620.82000000000005);
		int8_t* L_597 = L_596;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_597, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)37)), 8))))) = (double)(621.95000000000005);
		int8_t* L_598 = L_597;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_598, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)38)), 8))))) = (double)(622.98000000000002);
		int8_t* L_599 = L_598;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_599, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)39)), 8))))) = (double)(623.92999999999995);
		int8_t* L_600 = L_599;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_600, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)40)), 8))))) = (double)(624.78999999999996);
		int8_t* L_601 = L_600;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_601, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)41)), 8))))) = (double)(625.57000000000005);
		int8_t* L_602 = L_601;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_602, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)42)), 8))))) = (double)(626.27999999999997);
		int8_t* L_603 = L_602;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_603, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)43)), 8))))) = (double)(626.94000000000005);
		int8_t* L_604 = L_603;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_604, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)44)), 8))))) = (double)(627.52999999999997);
		int8_t* L_605 = L_604;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_605, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)45)), 8))))) = (double)(628.08000000000004);
		int8_t* L_606 = L_605;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_606, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)46)), 8))))) = (double)(628.57000000000005);
		int8_t* L_607 = L_606;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_607, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)47)), 8))))) = (double)(629.02999999999997);
		int8_t* L_608 = L_607;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_608, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)48)), 8))))) = (double)(629.44000000000005);
		int8_t* L_609 = L_608;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_609, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)49)), 8))))) = (double)(629.82000000000005);
		int8_t* L_610 = L_609;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_610, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)50)), 8))))) = (double)(630.16999999999996);
		int8_t* L_611 = L_610;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_611, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)51)), 8))))) = (double)(630.49000000000001);
		int8_t* L_612 = L_611;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_612, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)52)), 8))))) = (double)(630.77999999999997);
		int8_t* L_613 = L_612;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_613, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)53)), 8))))) = (double)(631.03999999999996);
		int8_t* L_614 = L_613;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_614, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)54)), 8))))) = (double)(631.27999999999997);
		int8_t* L_615 = L_614;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_615, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)55)), 8))))) = (double)(631.5);
		int8_t* L_616 = L_615;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_616, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)56)), 8))))) = (double)(631.71000000000004);
		int8_t* L_617 = L_616;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_617, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)57)), 8))))) = (double)(631.88999999999999);
		int8_t* L_618 = L_617;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_618, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)58)), 8))))) = (double)(632.05999999999995);
		int8_t* L_619 = L_618;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_619, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)59)), 8))))) = (double)(632.22000000000003);
		int8_t* L_620 = L_619;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_620, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)60)), 8))))) = (double)(632.36000000000001);
		int8_t* L_621 = L_620;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_621, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)61)), 8))))) = (double)(632.49000000000001);
		int8_t* L_622 = L_621;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_622, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)62)), 8))))) = (double)(632.61000000000001);
		int8_t* L_623 = L_622;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_623, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)63)), 8))))) = (double)(632.72000000000003);
		int8_t* L_624 = L_623;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_624, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)64)), 8))))) = (double)(632.82000000000005);
		int8_t* L_625 = L_624;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_625, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)65)), 8))))) = (double)(632.90999999999997);
		int8_t* L_626 = L_625;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_626, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)66)), 8))))) = (double)(633.0);
		int8_t* L_627 = L_626;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_627, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)67)), 8))))) = (double)(633.07000000000005);
		int8_t* L_628 = L_627;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_628, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)68)), 8))))) = (double)(633.13999999999999);
		int8_t* L_629 = L_628;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_629, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)69)), 8))))) = (double)(633.21000000000004);
		int8_t* L_630 = L_629;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_630, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)70)), 8))))) = (double)(633.26999999999998);
		int8_t* L_631 = L_630;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_631, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)71)), 8))))) = (double)(633.32000000000005);
		int8_t* L_632 = L_631;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_632, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)72)), 8))))) = (double)(633.37);
		int8_t* L_633 = L_632;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_633, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)73)), 8))))) = (double)(633.41999999999996);
		int8_t* L_634 = L_633;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_634, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)74)), 8))))) = (double)(633.46000000000004);
		int8_t* L_635 = L_634;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_635, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)75)), 8))))) = (double)(633.5);
		int8_t* L_636 = L_635;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_636, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)76)), 8))))) = (double)(633.52999999999997);
		int8_t* L_637 = L_636;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_637, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)77)), 8))))) = (double)(633.57000000000005);
		int8_t* L_638 = L_637;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_638, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)78)), 8))))) = (double)(633.60000000000002);
		int8_t* L_639 = L_638;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_639, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)79)), 8))))) = (double)(633.62);
		int8_t* L_640 = L_639;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_640, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)80)), 8))))) = (double)(633.64999999999998);
		int8_t* L_641 = L_640;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_641, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)81)), 8))))) = (double)(633.66999999999996);
		int8_t* L_642 = L_641;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_642, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)82)), 8))))) = (double)(633.69000000000005);
		int8_t* L_643 = L_642;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_643, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)83)), 8))))) = (double)(633.71000000000004);
		int8_t* L_644 = L_643;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_644, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)84)), 8))))) = (double)(633.73000000000002);
		int8_t* L_645 = L_644;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_645, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)85)), 8))))) = (double)(633.75);
		int8_t* L_646 = L_645;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_646, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)86)), 8))))) = (double)(633.75999999999999);
		int8_t* L_647 = L_646;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_647, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)87)), 8))))) = (double)(633.77999999999997);
		int8_t* L_648 = L_647;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_648, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)88)), 8))))) = (double)(633.78999999999996);
		int8_t* L_649 = L_648;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_649, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)89)), 8))))) = (double)(633.79999999999995);
		int8_t* L_650 = L_649;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_650, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)90)), 8))))) = (double)(633.80999999999995);
		int8_t* L_651 = L_650;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_651, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)91)), 8))))) = (double)(633.82000000000005);
		int8_t* L_652 = L_651;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_652, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)92)), 8))))) = (double)(633.83000000000004);
		int8_t* L_653 = L_652;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_653, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)93)), 8))))) = (double)(633.84000000000003);
		int8_t* L_654 = L_653;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_654, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)94)), 8))))) = (double)(633.85000000000002);
		int8_t* L_655 = L_654;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_655, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)95)), 8))))) = (double)(633.85000000000002);
		int8_t* L_656 = L_655;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_656, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)96)), 8))))) = (double)(633.86000000000001);
		int8_t* L_657 = L_656;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_657, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)97)), 8))))) = (double)(633.87);
		int8_t* L_658 = L_657;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_658, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)98)), 8))))) = (double)(633.87);
		int8_t* L_659 = L_658;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_659, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)99)), 8))))) = (double)(633.88);
		int8_t* L_660 = L_659;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_660, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)100)), 8))))) = (double)(633.88);
		int8_t* L_661 = L_660;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_661, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)101)), 8))))) = (double)(633.88);
		int8_t* L_662 = L_661;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_662, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)102)), 8))))) = (double)(633.88999999999999);
		int8_t* L_663 = L_662;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_663, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)103)), 8))))) = (double)(633.88999999999999);
		int8_t* L_664 = L_663;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_664, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)104)), 8))))) = (double)(633.89999999999998);
		int8_t* L_665 = L_664;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_665, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)105)), 8))))) = (double)(633.89999999999998);
		int8_t* L_666 = L_665;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_666, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)106)), 8))))) = (double)(633.89999999999998);
		int8_t* L_667 = L_666;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_667, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)107)), 8))))) = (double)(633.89999999999998);
		int8_t* L_668 = L_667;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_668, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)108)), 8))))) = (double)(633.90999999999997);
		int8_t* L_669 = L_668;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_669, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)109)), 8))))) = (double)(633.90999999999997);
		int8_t* L_670 = L_669;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_670, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)110)), 8))))) = (double)(633.90999999999997);
		int8_t* L_671 = L_670;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_671, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)111)), 8))))) = (double)(633.90999999999997);
		int8_t* L_672 = L_671;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_672, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)112)), 8))))) = (double)(633.90999999999997);
		int8_t* L_673 = L_672;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_673, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)113)), 8))))) = (double)(633.91999999999996);
		int8_t* L_674 = L_673;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_674, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)114)), 8))))) = (double)(633.91999999999996);
		int8_t* L_675 = L_674;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_675, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)115)), 8))))) = (double)(633.91999999999996);
		int8_t* L_676 = L_675;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_676, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)116)), 8))))) = (double)(633.91999999999996);
		int8_t* L_677 = L_676;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_677, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)117)), 8))))) = (double)(633.91999999999996);
		int8_t* L_678 = L_677;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_678, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)118)), 8))))) = (double)(633.91999999999996);
		int8_t* L_679 = L_678;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_679, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)119)), 8))))) = (double)(633.91999999999996);
		int8_t* L_680 = L_679;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_680, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)120)), 8))))) = (double)(633.91999999999996);
		int8_t* L_681 = L_680;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_681, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)121)), 8))))) = (double)(633.92999999999995);
		int8_t* L_682 = L_681;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_682, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)122)), 8))))) = (double)(633.92999999999995);
		int8_t* L_683 = L_682;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_683, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)123)), 8))))) = (double)(633.92999999999995);
		int8_t* L_684 = L_683;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_684, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)124)), 8))))) = (double)(633.92999999999995);
		int8_t* L_685 = L_684;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_685, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)125)), 8))))) = (double)(633.92999999999995);
		int8_t* L_686 = L_685;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_686, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)126)), 8))))) = (double)(633.92999999999995);
		int8_t* L_687 = L_686;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_687, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)127)), 8))))) = (double)(633.92999999999995);
		int8_t* L_688 = L_687;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_688, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)128)), 8))))) = (double)(633.92999999999995);
		int8_t* L_689 = L_688;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_689, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)129)), 8))))) = (double)(633.92999999999995);
		int8_t* L_690 = L_689;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_690, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)130)), 8))))) = (double)(633.92999999999995);
		int8_t* L_691 = L_690;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_691, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)131)), 8))))) = (double)(633.92999999999995);
		int8_t* L_692 = L_691;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_692, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)132)), 8))))) = (double)(633.92999999999995);
		int8_t* L_693 = L_692;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_693, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)133)), 8))))) = (double)(633.92999999999995);
		int8_t* L_694 = L_693;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_694, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)134)), 8))))) = (double)(633.92999999999995);
		int8_t* L_695 = L_694;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_695, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)135)), 8))))) = (double)(633.92999999999995);
		int8_t* L_696 = L_695;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_696, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)136)), 8))))) = (double)(633.92999999999995);
		int8_t* L_697 = L_696;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_697, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)137)), 8))))) = (double)(633.92999999999995);
		int8_t* L_698 = L_697;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_698, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)138)), 8))))) = (double)(633.92999999999995);
		int8_t* L_699 = L_698;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_699, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)139)), 8))))) = (double)(633.92999999999995);
		int8_t* L_700 = L_699;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_700, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)140)), 8))))) = (double)(633.92999999999995);
		int8_t* L_701 = L_700;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_701, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)141)), 8))))) = (double)(633.92999999999995);
		int8_t* L_702 = L_701;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_702, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)142)), 8))))) = (double)(633.92999999999995);
		int8_t* L_703 = L_702;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_703, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)143)), 8))))) = (double)(633.92999999999995);
		int8_t* L_704 = L_703;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_704, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)144)), 8))))) = (double)(633.92999999999995);
		int8_t* L_705 = L_704;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_705, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)145)), 8))))) = (double)(633.92999999999995);
		int8_t* L_706 = L_705;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_706, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)146)), 8))))) = (double)(633.92999999999995);
		int8_t* L_707 = L_706;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_707, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)147)), 8))))) = (double)(633.92999999999995);
		int8_t* L_708 = L_707;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_708, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)148)), 8))))) = (double)(633.92999999999995);
		int8_t* L_709 = L_708;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_709, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)149)), 8))))) = (double)(633.92999999999995);
		int8_t* L_710 = L_709;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_710, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)150)), 8))))) = (double)(633.92999999999995);
		int8_t* L_711 = L_710;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_711, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)151)), 8))))) = (double)(633.92999999999995);
		int8_t* L_712 = L_711;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_712, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)152)), 8))))) = (double)(633.92999999999995);
		int8_t* L_713 = L_712;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_713, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)153)), 8))))) = (double)(633.92999999999995);
		int8_t* L_714 = L_713;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_714, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)154)), 8))))) = (double)(633.92999999999995);
		int8_t* L_715 = L_714;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_715, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)155)), 8))))) = (double)(633.92999999999995);
		int8_t* L_716 = L_715;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_716, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)156)), 8))))) = (double)(633.92999999999995);
		int8_t* L_717 = L_716;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_717, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)157)), 8))))) = (double)(633.92999999999995);
		int8_t* L_718 = L_717;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_718, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)158)), 8))))) = (double)(633.92999999999995);
		int8_t* L_719 = L_718;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_719, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)159)), 8))))) = (double)(633.92999999999995);
		int8_t* L_720 = L_719;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_720, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)160)), 8))))) = (double)(633.92999999999995);
		int8_t* L_721 = L_720;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_721, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)161)), 8))))) = (double)(633.92999999999995);
		int8_t* L_722 = L_721;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_722, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)162)), 8))))) = (double)(633.92999999999995);
		int8_t* L_723 = L_722;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_723, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)163)), 8))))) = (double)(633.92999999999995);
		int8_t* L_724 = L_723;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_724, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)164)), 8))))) = (double)(633.92999999999995);
		int8_t* L_725 = L_724;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_725, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)165)), 8))))) = (double)(633.94000000000005);
		int8_t* L_726 = L_725;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_726, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)166)), 8))))) = (double)(633.94000000000005);
		int8_t* L_727 = L_726;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_727, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)167)), 8))))) = (double)(633.94000000000005);
		int8_t* L_728 = L_727;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_728, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)168)), 8))))) = (double)(633.94000000000005);
		int8_t* L_729 = L_728;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_729, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)169)), 8))))) = (double)(633.94000000000005);
		int8_t* L_730 = L_729;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_730, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)170)), 8))))) = (double)(633.94000000000005);
		int8_t* L_731 = L_730;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_731, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)171)), 8))))) = (double)(633.94000000000005);
		int8_t* L_732 = L_731;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_732, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)172)), 8))))) = (double)(633.94000000000005);
		int8_t* L_733 = L_732;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_733, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)173)), 8))))) = (double)(633.94000000000005);
		int8_t* L_734 = L_733;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_734, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)174)), 8))))) = (double)(633.94000000000005);
		int8_t* L_735 = L_734;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_735, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)175)), 8))))) = (double)(633.94000000000005);
		int8_t* L_736 = L_735;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_736, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)176)), 8))))) = (double)(633.94000000000005);
		int8_t* L_737 = L_736;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_737, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)177)), 8))))) = (double)(633.94000000000005);
		int8_t* L_738 = L_737;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_738, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)178)), 8))))) = (double)(633.94000000000005);
		int8_t* L_739 = L_738;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_739, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)179)), 8))))) = (double)(633.94000000000005);
		int8_t* L_740 = L_739;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_740, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)180)), 8))))) = (double)(633.94000000000005);
		int8_t* L_741 = L_740;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_741, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)181)), 8))))) = (double)(633.94000000000005);
		int8_t* L_742 = L_741;
		*((double*)((int8_t*)il2cpp_codegen_add((intptr_t)L_742, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)((int32_t)182)), 8))))) = (double)(633.94000000000005);
		Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5 L_743;
		memset((&L_743), 0, sizeof(L_743));
		Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_inline((&L_743), (void*)L_742, ((int32_t)183), Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_RuntimeMethod_var);
		V_3 = L_743;
		double L_744 = ___0_z;
		if ((!(((double)L_744) < ((double)(90000.0)))))
		{
			goto IL_337b;
		}
	}
	{
		V_4 = (86000.0);
		double L_745 = ___1_t86;
		V_5 = L_745;
		double L_746 = ___2_p86;
		V_6 = L_746;
		double L_747 = ___2_p86;
		double L_748 = ___1_t86;
		V_7 = ((double)(L_747/((double)il2cpp_codegen_multiply((287.053), L_748))));
		double L_749 = ___1_t86;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_750;
		L_750 = sqrt(((double)il2cpp_codegen_multiply((401.87419999999997), L_749)));
		V_8 = L_750;
		V_9 = 0;
		goto IL_33f5;
	}

IL_337b:
	{
		double L_751 = ___0_z;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_752;
		L_752 = floor(((double)(((double)il2cpp_codegen_subtract(L_751, (90000.0)))/(5000.0))));
		double L_753;
		L_753 = Math_Min_mA3310F1FF7876DA2FC7F37B822E6DD66410565C1(L_752, (181.0), NULL);
		V_13 = il2cpp_codegen_cast_double_to_int<int32_t>(L_753);
		int32_t L_754 = V_13;
		V_4 = ((double)il2cpp_codegen_add((90000.0), ((double)il2cpp_codegen_multiply(((double)L_754), (5000.0)))));
		int32_t L_755 = V_13;
		double* L_756;
		L_756 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_0))->____pointer))->value, (L_755), ((&V_0))->____length);
		double L_757 = *((double*)L_756);
		V_5 = L_757;
		int32_t L_758 = V_13;
		double* L_759;
		L_759 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_1))->____pointer))->value, (L_758), ((&V_1))->____length);
		double L_760 = *((double*)L_759);
		V_6 = L_760;
		int32_t L_761 = V_13;
		double* L_762;
		L_762 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_2))->____pointer))->value, (L_761), ((&V_2))->____length);
		double L_763 = *((double*)L_762);
		V_7 = L_763;
		int32_t L_764 = V_13;
		double* L_765;
		L_765 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_3))->____pointer))->value, (L_764), ((&V_3))->____length);
		double L_766 = *((double*)L_765);
		V_8 = L_766;
		int32_t L_767 = V_13;
		V_9 = ((int32_t)il2cpp_codegen_add(L_767, 1));
	}

IL_33f5:
	{
		int32_t L_768 = V_9;
		V_10 = ((double)il2cpp_codegen_add((90000.0), ((double)il2cpp_codegen_multiply(((double)L_768), (5000.0)))));
		double L_769 = ___0_z;
		double L_770 = V_4;
		double L_771 = V_10;
		double L_772 = V_4;
		V_11 = ((double)(((double)il2cpp_codegen_subtract(L_769, L_770))/((double)il2cpp_codegen_subtract(L_771, L_772))));
		double L_773 = V_11;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_774;
		L_774 = Math_Min_mA3310F1FF7876DA2FC7F37B822E6DD66410565C1(L_773, (1.0), NULL);
		V_12 = L_774;
		il2cpp_codegen_initobj((&V_14), sizeof(AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1));
		double L_775 = V_5;
		int32_t L_776 = V_9;
		double* L_777;
		L_777 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_0))->____pointer))->value, (L_776), ((&V_0))->____length);
		double L_778 = *((double*)L_777);
		double L_779 = V_5;
		double L_780 = V_12;
		(&V_14)->___T = ((double)il2cpp_codegen_add(L_775, ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_subtract(L_778, L_779)), L_780))));
		double L_781 = V_6;
		double L_782;
		L_782 = log(L_781);
		int32_t L_783 = V_9;
		double* L_784;
		L_784 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_1))->____pointer))->value, (L_783), ((&V_1))->____length);
		double L_785 = *((double*)L_784);
		double L_786;
		L_786 = log(L_785);
		double L_787 = V_6;
		double L_788;
		L_788 = log(L_787);
		double L_789 = V_11;
		double L_790;
		L_790 = exp(((double)il2cpp_codegen_add(L_782, ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_subtract(L_786, L_788)), L_789)))));
		(&V_14)->___p = L_790;
		double L_791 = V_7;
		double L_792;
		L_792 = log(L_791);
		int32_t L_793 = V_9;
		double* L_794;
		L_794 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_2))->____pointer))->value, (L_793), ((&V_2))->____length);
		double L_795 = *((double*)L_794);
		double L_796;
		L_796 = log(L_795);
		double L_797 = V_7;
		double L_798;
		L_798 = log(L_797);
		double L_799 = V_11;
		double L_800;
		L_800 = exp(((double)il2cpp_codegen_add(L_792, ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_subtract(L_796, L_798)), L_799)))));
		(&V_14)->___rho = L_800;
		double L_801 = V_8;
		int32_t L_802 = V_9;
		double* L_803;
		L_803 = il2cpp_span_get_item((double*)((Il2CppByReference*)&(((&V_3))->____pointer))->value, (L_802), ((&V_3))->____length);
		double L_804 = *((double*)L_803);
		double L_805 = V_8;
		double L_806 = V_12;
		(&V_14)->___a = ((double)il2cpp_codegen_add(L_801, ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_subtract(L_804, L_805)), L_806))));
		AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 L_807 = V_14;
		return L_807;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Atmosphere_SampleDensity_mEE9E9E2AAB60F1889CB2AEC36D9B3CF43B7083ED (DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___0_altitudeM, DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___1_rho, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Action_1_tD69A6DC9FBE94131E52F5A73B2A9D4AB51EEC404_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Parallel_tEDD920942C8F814D55C6B029EACB00C81620BD14_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&U3CU3Ec__DisplayClass13_0_U3CSampleDensityU3Eb__0_m25010DB8E12C39982D936438B2B5A9E9A4635998_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973* V_0 = NULL;
	int32_t V_1 = 0;
	{
		U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973* L_0 = (U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973*)il2cpp_codegen_object_new(U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973_il2cpp_TypeInfo_var);
		U3CU3Ec__DisplayClass13_0__ctor_m75AF0DCA50154BA312BF33D3403D7B3F289BD364(L_0, NULL);
		V_0 = L_0;
		U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973* L_1 = V_0;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_2 = ___1_rho;
		NullCheck(L_1);
		L_1->___rho = L_2;
		Il2CppCodeGenWriteBarrier((void**)(&L_1->___rho), (void*)L_2);
		U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973* L_3 = V_0;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_4 = ___0_altitudeM;
		NullCheck(L_3);
		L_3->___altitudeM = L_4;
		Il2CppCodeGenWriteBarrier((void**)(&L_3->___altitudeM), (void*)L_4);
		U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973* L_5 = V_0;
		NullCheck(L_5);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_6 = L_5->___altitudeM;
		if (L_6)
		{
			goto IL_0027;
		}
	}
	{
		ArgumentNullException_t327031E412FAB2351B0022DD5DAD47E67E597129* L_7 = (ArgumentNullException_t327031E412FAB2351B0022DD5DAD47E67E597129*)il2cpp_codegen_object_new(((RuntimeClass*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&ArgumentNullException_t327031E412FAB2351B0022DD5DAD47E67E597129_il2cpp_TypeInfo_var)));
		ArgumentNullException__ctor_m444AE141157E333844FC1A9500224C2F9FD24F4B(L_7, ((String_t*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&_stringLiteral0807218A785BE305BE2414A2A176495BD6EFBF7E)), NULL);
		IL2CPP_RAISE_MANAGED_EXCEPTION(L_7, ((RuntimeMethod*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&Atmosphere_SampleDensity_mEE9E9E2AAB60F1889CB2AEC36D9B3CF43B7083ED_RuntimeMethod_var)));
	}

IL_0027:
	{
		U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973* L_8 = V_0;
		NullCheck(L_8);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_9 = L_8->___rho;
		if (!L_9)
		{
			goto IL_0041;
		}
	}
	{
		U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973* L_10 = V_0;
		NullCheck(L_10);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_11 = L_10->___rho;
		NullCheck(L_11);
		U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973* L_12 = V_0;
		NullCheck(L_12);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_13 = L_12->___altitudeM;
		NullCheck(L_13);
		if ((((int32_t)((int32_t)(((RuntimeArray*)L_11)->max_length))) == ((int32_t)((int32_t)(((RuntimeArray*)L_13)->max_length)))))
		{
			goto IL_004c;
		}
	}

IL_0041:
	{
		ArgumentException_tAD90411542A20A9C72D5CDA3A84181D8B947A263* L_14 = (ArgumentException_tAD90411542A20A9C72D5CDA3A84181D8B947A263*)il2cpp_codegen_object_new(((RuntimeClass*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&ArgumentException_tAD90411542A20A9C72D5CDA3A84181D8B947A263_il2cpp_TypeInfo_var)));
		ArgumentException__ctor_m026938A67AF9D36BB7ED27F80425D7194B514465(L_14, ((String_t*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&_stringLiteral5574CCCEF712B5D9AF4D4A9BC634BC330C41F6EE)), NULL);
		IL2CPP_RAISE_MANAGED_EXCEPTION(L_14, ((RuntimeMethod*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&Atmosphere_SampleDensity_mEE9E9E2AAB60F1889CB2AEC36D9B3CF43B7083ED_RuntimeMethod_var)));
	}

IL_004c:
	{
		U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973* L_15 = V_0;
		NullCheck(L_15);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_16 = L_15->___altitudeM;
		NullCheck(L_16);
		if ((((int32_t)((int32_t)(((RuntimeArray*)L_16)->max_length))) < ((int32_t)((int32_t)32))))
		{
			goto IL_0074;
		}
	}
	{
		U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973* L_17 = V_0;
		NullCheck(L_17);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_18 = L_17->___altitudeM;
		NullCheck(L_18);
		U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973* L_19 = V_0;
		Action_1_tD69A6DC9FBE94131E52F5A73B2A9D4AB51EEC404* L_20 = (Action_1_tD69A6DC9FBE94131E52F5A73B2A9D4AB51EEC404*)il2cpp_codegen_object_new(Action_1_tD69A6DC9FBE94131E52F5A73B2A9D4AB51EEC404_il2cpp_TypeInfo_var);
		Action_1__ctor_m1BA854F3F4319EA4A4294DDFDA21C395B8D0FF87(L_20, L_19, (intptr_t)((void*)U3CU3Ec__DisplayClass13_0_U3CSampleDensityU3Eb__0_m25010DB8E12C39982D936438B2B5A9E9A4635998_RuntimeMethod_var), NULL);
		il2cpp_codegen_runtime_class_init_inline(Parallel_tEDD920942C8F814D55C6B029EACB00C81620BD14_il2cpp_TypeInfo_var);
		ParallelLoopResult_t9C383DA248DA9082ECBF057DE0879AB99EE6A734 L_21;
		L_21 = Parallel_For_mD3332AD2F9C519769F2BEF201C54F3C57EFCF6EC(0, ((int32_t)(((RuntimeArray*)L_18)->max_length)), L_20, NULL);
		return;
	}

IL_0074:
	{
		V_1 = 0;
		goto IL_0096;
	}

IL_0078:
	{
		U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973* L_22 = V_0;
		NullCheck(L_22);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_23 = L_22->___rho;
		int32_t L_24 = V_1;
		U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973* L_25 = V_0;
		NullCheck(L_25);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_26 = L_25->___altitudeM;
		int32_t L_27 = V_1;
		NullCheck(L_26);
		int32_t L_28 = L_27;
		double L_29 = (L_26)->GetAt(static_cast<il2cpp_array_size_t>(L_28));
		AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 L_30;
		L_30 = Atmosphere_At_mA6E038B8FA424E44AD2A97805AC95A264333ECF7(L_29, NULL);
		double L_31 = L_30.___rho;
		NullCheck(L_23);
		(L_23)->SetAt(static_cast<il2cpp_array_size_t>(L_24), (double)L_31);
		int32_t L_32 = V_1;
		V_1 = ((int32_t)il2cpp_codegen_add(L_32, 1));
	}

IL_0096:
	{
		int32_t L_33 = V_1;
		U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973* L_34 = V_0;
		NullCheck(L_34);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_35 = L_34->___altitudeM;
		NullCheck(L_35);
		if ((((int32_t)L_33) < ((int32_t)((int32_t)(((RuntimeArray*)L_35)->max_length)))))
		{
			goto IL_0078;
		}
	}
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Atmosphere_PressureAt_m9FF6D0793A84BD89EE40F20B9DB821507C56375A (double ___0_z, const RuntimeMethod* method) 
{
	{
		double L_0 = ___0_z;
		AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 L_1;
		L_1 = Atmosphere_At_mA6E038B8FA424E44AD2A97805AC95A264333ECF7(L_0, NULL);
		double L_2 = L_1.___p;
		return L_2;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Atmosphere_DensityAt_mF54A7B213A3631F8C78E2530B16AB2E1813EC6CD (double ___0_z, const RuntimeMethod* method) 
{
	{
		double L_0 = ___0_z;
		AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 L_1;
		L_1 = Atmosphere_At_mA6E038B8FA424E44AD2A97805AC95A264333ECF7(L_0, NULL);
		double L_2 = L_1.___rho;
		return L_2;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void U3CU3Ec__DisplayClass13_0__ctor_m75AF0DCA50154BA312BF33D3403D7B3F289BD364 (U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973* __this, const RuntimeMethod* method) 
{
	{
		Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2(__this, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void U3CU3Ec__DisplayClass13_0_U3CSampleDensityU3Eb__0_m25010DB8E12C39982D936438B2B5A9E9A4635998 (U3CU3Ec__DisplayClass13_0_t9863FD73ECD7B774A5ABDB320B522D174B684973* __this, int32_t ___0_i, const RuntimeMethod* method) 
{
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_0 = __this->___rho;
		int32_t L_1 = ___0_i;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_2 = __this->___altitudeM;
		int32_t L_3 = ___0_i;
		NullCheck(L_2);
		int32_t L_4 = L_3;
		double L_5 = (L_2)->GetAt(static_cast<il2cpp_array_size_t>(L_4));
		AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 L_6;
		L_6 = Atmosphere_At_mA6E038B8FA424E44AD2A97805AC95A264333ECF7(L_5, NULL);
		double L_7 = L_6.___rho;
		NullCheck(L_0);
		(L_0)->SetAt(static_cast<il2cpp_array_size_t>(L_1), (double)L_7);
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Frames_get_Flattening_m542DC04E96C01FEADD5DB997034C42EF80C3F3E9 (const RuntimeMethod* method) 
{
	{
		return (0.0033528106647474805);
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Frames_JdUt1Parts_m600EE3044F3D0E1F3B1EA85B22EE165DF6DF13CA (int32_t ___0_year, int32_t ___1_month, int32_t ___2_day, double ___3_hoursUtc, double* ___4_jd1, double* ___5_jd2, const RuntimeMethod* method) 
{
	{
		double* L_0 = ___4_jd1;
		int32_t L_1 = ___0_year;
		int32_t L_2 = ___1_month;
		int32_t L_3 = ___2_day;
		double L_4;
		L_4 = Time_JulianDate_m594F1315B9A1CA84998225A0B0927C391896349F(L_1, L_2, L_3, (0.0), NULL);
		*((double*)L_0) = (double)L_4;
		double* L_5 = ___5_jd2;
		double L_6 = ___3_hoursUtc;
		*((double*)L_5) = (double)((double)(L_6/(24.0)));
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Frames_JdUt1FromUtc_mA9411641ABC7F88C3E61E7063B159849CF359C7A (int32_t ___0_year, int32_t ___1_month, int32_t ___2_day, double ___3_hoursUtc, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = ___0_year;
		int32_t L_1 = ___1_month;
		int32_t L_2 = ___2_day;
		double L_3 = ___3_hoursUtc;
		double L_4;
		L_4 = Time_JulianDate_m594F1315B9A1CA84998225A0B0927C391896349F(L_0, L_1, L_2, L_3, NULL);
		return L_4;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Frames_EraRad_mF0F9486708B63E13F3B962169AF7BC5CC07F6815 (double ___0_jd1, double ___1_jd2, const RuntimeMethod* method) 
{
	double V_0 = 0.0;
	double V_1 = 0.0;
	double V_2 = 0.0;
	{
		double L_0 = ___0_jd1;
		double L_1 = ___1_jd2;
		if ((!(((double)L_0) < ((double)L_1))))
		{
			goto IL_000a;
		}
	}
	{
		double L_2 = ___0_jd1;
		V_0 = L_2;
		double L_3 = ___1_jd2;
		V_1 = L_3;
		goto IL_000e;
	}

IL_000a:
	{
		double L_4 = ___1_jd2;
		V_0 = L_4;
		double L_5 = ___0_jd1;
		V_1 = L_5;
	}

IL_000e:
	{
		double L_6 = V_0;
		double L_7 = V_1;
		V_2 = ((double)il2cpp_codegen_add(L_6, ((double)il2cpp_codegen_subtract(L_7, (2451545.0)))));
		double L_8 = V_0;
		double L_9;
		L_9 = Frames_Fmod_mDFE24EF88575C8349A26838B511A8A88FA506D53(L_8, (1.0), NULL);
		double L_10 = V_1;
		double L_11;
		L_11 = Frames_Fmod_mDFE24EF88575C8349A26838B511A8A88FA506D53(L_10, (1.0), NULL);
		double L_12 = V_2;
		double L_13;
		L_13 = Frames_WrapTwoPi_mEEB242735CB58B363C041EE179669F4033C5C5AA(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_add(((double)il2cpp_codegen_add(((double)il2cpp_codegen_add(L_9, L_11)), (0.77905727326399998))), ((double)il2cpp_codegen_multiply((0.0027378119113544799), L_12)))), (2.0))), (3.1415926535897931))), NULL);
		return L_13;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Frames_EraSiderealSeconds_mA0696213306BBDBC7B4FE885DF26F356352F3E88 (double ___0_jd1, double ___1_jd2, const RuntimeMethod* method) 
{
	{
		double L_0 = ___0_jd1;
		double L_1 = ___1_jd2;
		double L_2;
		L_2 = Frames_EraRad_mF0F9486708B63E13F3B962169AF7BC5CC07F6815(L_0, L_1, NULL);
		return ((double)(((double)il2cpp_codegen_multiply(L_2, (86400.0)))/(6.2831853071795862)));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Frames_Fmod_mDFE24EF88575C8349A26838B511A8A88FA506D53 (double ___0_x, double ___1_y, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		double L_0 = ___0_x;
		double L_1 = ___0_x;
		double L_2 = ___1_y;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_3;
		L_3 = Math_Truncate_mE66B1AD68C17D27675DE0CB74643374F9EDB649C(((double)(L_1/L_2)), NULL);
		double L_4 = ___1_y;
		return ((double)il2cpp_codegen_subtract(L_0, ((double)il2cpp_codegen_multiply(L_3, L_4))));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Frames_WrapTwoPi_mEEB242735CB58B363C041EE179669F4033C5C5AA (double ___0_ang, const RuntimeMethod* method) 
{
	double V_0 = 0.0;
	{
		V_0 = (6.2831853071795862);
		double L_0 = ___0_ang;
		double L_1 = V_0;
		___0_ang = (fmod(L_0, L_1));
		double L_2 = ___0_ang;
		if ((!(((double)L_2) < ((double)(0.0)))))
		{
			goto IL_0020;
		}
	}
	{
		double L_3 = ___0_ang;
		double L_4 = V_0;
		___0_ang = ((double)il2cpp_codegen_add(L_3, L_4));
	}

IL_0020:
	{
		double L_5 = ___0_ang;
		return L_5;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Frames_GeodeticToEcef_m38D5FAB1CACC71E28477B5D6B35B2CAEA874924F (double ___0_latRad, double ___1_lonRad, double ___2_h, double* ___3_x, double* ___4_y, double* ___5_z, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	double V_0 = 0.0;
	double V_1 = 0.0;
	double V_2 = 0.0;
	double V_3 = 0.0;
	{
		double L_0;
		L_0 = Frames_get_Flattening_m542DC04E96C01FEADD5DB997034C42EF80C3F3E9(NULL);
		double L_1;
		L_1 = Frames_get_Flattening_m542DC04E96C01FEADD5DB997034C42EF80C3F3E9(NULL);
		V_0 = ((double)il2cpp_codegen_multiply(L_0, ((double)il2cpp_codegen_subtract((2.0), L_1))));
		double L_2 = ___0_latRad;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_3;
		L_3 = sin(L_2);
		V_1 = L_3;
		double L_4 = ___0_latRad;
		double L_5;
		L_5 = cos(L_4);
		V_2 = L_5;
		double L_6 = V_0;
		double L_7 = V_1;
		double L_8 = V_1;
		double L_9;
		L_9 = sqrt(((double)il2cpp_codegen_subtract((1.0), ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_6, L_7)), L_8)))));
		V_3 = ((double)((6378137.0)/L_9));
		double* L_10 = ___3_x;
		double L_11 = V_3;
		double L_12 = ___2_h;
		double L_13 = V_2;
		double L_14 = ___1_lonRad;
		double L_15;
		L_15 = cos(L_14);
		*((double*)L_10) = (double)((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_add(L_11, L_12)), L_13)), L_15));
		double* L_16 = ___4_y;
		double L_17 = V_3;
		double L_18 = ___2_h;
		double L_19 = V_2;
		double L_20 = ___1_lonRad;
		double L_21;
		L_21 = sin(L_20);
		*((double*)L_16) = (double)((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_add(L_17, L_18)), L_19)), L_21));
		double* L_22 = ___5_z;
		double L_23 = V_3;
		double L_24 = V_0;
		double L_25 = ___2_h;
		double L_26 = V_1;
		*((double*)L_22) = (double)((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_23, ((double)il2cpp_codegen_subtract((1.0), L_24)))), L_25)), L_26));
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Frames_EcefToGeodetic_m7DD624A0DAFCD2DF881B767F230C9C2707A23F53 (double ___0_x, double ___1_y, double ___2_z, double* ___3_latRad, double* ___4_lonRad, double* ___5_h, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	double V_0 = 0.0;
	double V_1 = 0.0;
	double V_2 = 0.0;
	double V_3 = 0.0;
	double V_4 = 0.0;
	double V_5 = 0.0;
	double V_6 = 0.0;
	double V_7 = 0.0;
	double V_8 = 0.0;
	double V_9 = 0.0;
	int32_t V_10 = 0;
	double V_11 = 0.0;
	double V_12 = 0.0;
	double* G_B3_0 = NULL;
	double* G_B2_0 = NULL;
	double G_B4_0 = 0.0;
	double* G_B4_1 = NULL;
	{
		V_0 = (6378137.0);
		double L_0;
		L_0 = Frames_get_Flattening_m542DC04E96C01FEADD5DB997034C42EF80C3F3E9(NULL);
		V_1 = L_0;
		double L_1 = V_0;
		double L_2 = V_1;
		V_2 = ((double)il2cpp_codegen_multiply(L_1, ((double)il2cpp_codegen_subtract((1.0), L_2))));
		double L_3 = V_1;
		double L_4 = V_1;
		V_3 = ((double)il2cpp_codegen_multiply(L_3, ((double)il2cpp_codegen_subtract((2.0), L_4))));
		double L_5 = V_0;
		double L_6 = V_0;
		double L_7 = V_2;
		double L_8 = V_2;
		double L_9 = V_2;
		double L_10 = V_2;
		V_4 = ((double)(((double)il2cpp_codegen_subtract(((double)il2cpp_codegen_multiply(L_5, L_6)), ((double)il2cpp_codegen_multiply(L_7, L_8))))/((double)il2cpp_codegen_multiply(L_9, L_10))));
		double* L_11 = ___4_lonRad;
		double L_12 = ___1_y;
		double L_13 = ___0_x;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_14;
		L_14 = atan2(L_12, L_13);
		*((double*)L_11) = (double)L_14;
		double L_15 = ___0_x;
		double L_16 = ___0_x;
		double L_17 = ___1_y;
		double L_18 = ___1_y;
		double L_19;
		L_19 = sqrt(((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_15, L_16)), ((double)il2cpp_codegen_multiply(L_17, L_18)))));
		V_5 = L_19;
		double* L_20 = ___5_h;
		*((double*)L_20) = (double)(0.0);
		double L_21 = V_5;
		if ((!(((double)L_21) < ((double)(1.0E-08)))))
		{
			goto IL_0098;
		}
	}
	{
		double* L_22 = ___3_latRad;
		double L_23 = ___2_z;
		if ((((double)L_23) >= ((double)(0.0))))
		{
			G_B3_0 = L_22;
			goto IL_0082;
		}
		G_B2_0 = L_22;
	}
	{
		G_B4_0 = (-1.5707963267948966);
		G_B4_1 = G_B2_0;
		goto IL_008b;
	}

IL_0082:
	{
		G_B4_0 = (1.5707963267948966);
		G_B4_1 = G_B3_0;
	}

IL_008b:
	{
		*((double*)G_B4_1) = (double)G_B4_0;
		double* L_24 = ___5_h;
		double L_25 = ___2_z;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_26;
		L_26 = fabs(L_25);
		double L_27 = V_2;
		*((double*)L_24) = (double)((double)il2cpp_codegen_subtract(L_26, L_27));
		return;
	}

IL_0098:
	{
		double L_28 = ___2_z;
		double L_29 = V_0;
		double L_30 = V_5;
		double L_31 = V_2;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_32;
		L_32 = atan2(((double)il2cpp_codegen_multiply(L_28, L_29)), ((double)il2cpp_codegen_multiply(L_30, L_31)));
		double L_33 = L_32;
		double L_34;
		L_34 = sin(L_33);
		V_6 = L_34;
		double L_35;
		L_35 = cos(L_33);
		V_7 = L_35;
		double* L_36 = ___3_latRad;
		double L_37 = ___2_z;
		double L_38 = V_4;
		double L_39 = V_2;
		double L_40 = V_6;
		double L_41 = V_6;
		double L_42 = V_6;
		double L_43 = V_5;
		double L_44 = V_3;
		double L_45 = V_0;
		double L_46 = V_7;
		double L_47 = V_7;
		double L_48 = V_7;
		double L_49;
		L_49 = atan2(((double)il2cpp_codegen_add(L_37, ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_38, L_39)), L_40)), L_41)), L_42)))), ((double)il2cpp_codegen_subtract(L_43, ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_44, L_45)), L_46)), L_47)), L_48)))));
		*((double*)L_36) = (double)L_49;
		V_10 = 0;
		goto IL_0136;
	}

IL_00dd:
	{
		double* L_50 = ___3_latRad;
		double L_51 = *((double*)L_50);
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_52;
		L_52 = sin(L_51);
		V_11 = L_52;
		double L_53 = V_0;
		double L_54 = V_3;
		double L_55 = V_11;
		double L_56 = V_11;
		double L_57;
		L_57 = sqrt(((double)il2cpp_codegen_subtract((1.0), ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_54, L_55)), L_56)))));
		V_12 = ((double)(L_53/L_57));
		double* L_58 = ___5_h;
		double L_59 = V_5;
		double* L_60 = ___3_latRad;
		double L_61 = *((double*)L_60);
		double L_62;
		L_62 = cos(L_61);
		double L_63 = V_12;
		*((double*)L_58) = (double)((double)il2cpp_codegen_subtract(((double)(L_59/L_62)), L_63));
		double* L_64 = ___3_latRad;
		double L_65 = ___2_z;
		double L_66 = V_5;
		double L_67 = V_3;
		double L_68 = V_12;
		double L_69 = V_12;
		double* L_70 = ___5_h;
		double L_71 = *((double*)L_70);
		double L_72;
		L_72 = atan2(L_65, ((double)il2cpp_codegen_multiply(L_66, ((double)il2cpp_codegen_subtract((1.0), ((double)(((double)il2cpp_codegen_multiply(L_67, L_68))/((double)il2cpp_codegen_add(L_69, L_71)))))))));
		*((double*)L_64) = (double)L_72;
		int32_t L_73 = V_10;
		V_10 = ((int32_t)il2cpp_codegen_add(L_73, 1));
	}

IL_0136:
	{
		int32_t L_74 = V_10;
		if ((((int32_t)L_74) < ((int32_t)2)))
		{
			goto IL_00dd;
		}
	}
	{
		double* L_75 = ___3_latRad;
		double L_76 = *((double*)L_75);
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_77;
		L_77 = sin(L_76);
		V_8 = L_77;
		double L_78 = V_0;
		double L_79 = V_3;
		double L_80 = V_8;
		double L_81 = V_8;
		double L_82;
		L_82 = sqrt(((double)il2cpp_codegen_subtract((1.0), ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_79, L_80)), L_81)))));
		V_9 = ((double)(L_78/L_82));
		double* L_83 = ___5_h;
		double L_84 = V_5;
		double* L_85 = ___3_latRad;
		double L_86 = *((double*)L_85);
		double L_87;
		L_87 = cos(L_86);
		double L_88 = V_9;
		*((double*)L_83) = (double)((double)il2cpp_codegen_subtract(((double)(L_84/L_87)), L_88));
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Frames_EciToEcef_m221B6D94CB3D1CCD44AFC9359AFA324924D7F890 (double ___0_jd1, double ___1_jd2, double ___2_xi, double ___3_yi, double ___4_zi, double ___5_vxi, double ___6_vyi, double ___7_vzi, double* ___8_xe, double* ___9_ye, double* ___10_ze, double* ___11_vxe, double* ___12_vye, double* ___13_vze, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	double V_0 = 0.0;
	double V_1 = 0.0;
	double V_2 = 0.0;
	double V_3 = 0.0;
	double V_4 = 0.0;
	{
		double L_0 = ___0_jd1;
		double L_1 = ___1_jd2;
		double L_2;
		L_2 = Frames_EraRad_mF0F9486708B63E13F3B962169AF7BC5CC07F6815(L_0, L_1, NULL);
		double L_3 = L_2;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_4;
		L_4 = cos(L_3);
		V_0 = L_4;
		double L_5;
		L_5 = sin(L_3);
		V_1 = L_5;
		double* L_6 = ___8_xe;
		double L_7 = V_0;
		double L_8 = ___2_xi;
		double L_9 = V_1;
		double L_10 = ___3_yi;
		*((double*)L_6) = (double)((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_7, L_8)), ((double)il2cpp_codegen_multiply(L_9, L_10))));
		double* L_11 = ___9_ye;
		double L_12 = V_1;
		double L_13 = ___2_xi;
		double L_14 = V_0;
		double L_15 = ___3_yi;
		*((double*)L_11) = (double)((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(((-L_12)), L_13)), ((double)il2cpp_codegen_multiply(L_14, L_15))));
		double* L_16 = ___10_ze;
		double L_17 = ___4_zi;
		*((double*)L_16) = (double)L_17;
		double L_18 = V_0;
		double L_19 = ___5_vxi;
		double L_20 = V_1;
		double L_21 = ___6_vyi;
		V_2 = ((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_18, L_19)), ((double)il2cpp_codegen_multiply(L_20, L_21))));
		double L_22 = V_1;
		double L_23 = ___5_vxi;
		double L_24 = V_0;
		double L_25 = ___6_vyi;
		V_3 = ((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(((-L_22)), L_23)), ((double)il2cpp_codegen_multiply(L_24, L_25))));
		V_4 = (7.2921159000000004E-05);
		double* L_26 = ___11_vxe;
		double L_27 = V_2;
		double L_28 = V_4;
		double* L_29 = ___9_ye;
		double L_30 = *((double*)L_29);
		*((double*)L_26) = (double)((double)il2cpp_codegen_add(L_27, ((double)il2cpp_codegen_multiply(L_28, L_30))));
		double* L_31 = ___12_vye;
		double L_32 = V_3;
		double L_33 = V_4;
		double* L_34 = ___8_xe;
		double L_35 = *((double*)L_34);
		*((double*)L_31) = (double)((double)il2cpp_codegen_subtract(L_32, ((double)il2cpp_codegen_multiply(L_33, L_35))));
		double* L_36 = ___13_vze;
		double L_37 = ___7_vzi;
		*((double*)L_36) = (double)L_37;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Frames_EcefToEci_m21F3D42BF802ACACBD5D031EC959833DE18AE32A (double ___0_jd1, double ___1_jd2, double ___2_xe, double ___3_ye, double ___4_ze, double ___5_vxe, double ___6_vye, double ___7_vze, double* ___8_xi, double* ___9_yi, double* ___10_zi, double* ___11_vxi, double* ___12_vyi, double* ___13_vzi, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	double V_0 = 0.0;
	double V_1 = 0.0;
	double V_2 = 0.0;
	double V_3 = 0.0;
	double V_4 = 0.0;
	{
		double L_0 = ___0_jd1;
		double L_1 = ___1_jd2;
		double L_2;
		L_2 = Frames_EraRad_mF0F9486708B63E13F3B962169AF7BC5CC07F6815(L_0, L_1, NULL);
		double L_3 = L_2;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_4;
		L_4 = cos(L_3);
		V_0 = L_4;
		double L_5;
		L_5 = sin(L_3);
		V_1 = L_5;
		double* L_6 = ___8_xi;
		double L_7 = V_0;
		double L_8 = ___2_xe;
		double L_9 = V_1;
		double L_10 = ___3_ye;
		*((double*)L_6) = (double)((double)il2cpp_codegen_subtract(((double)il2cpp_codegen_multiply(L_7, L_8)), ((double)il2cpp_codegen_multiply(L_9, L_10))));
		double* L_11 = ___9_yi;
		double L_12 = V_1;
		double L_13 = ___2_xe;
		double L_14 = V_0;
		double L_15 = ___3_ye;
		*((double*)L_11) = (double)((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_12, L_13)), ((double)il2cpp_codegen_multiply(L_14, L_15))));
		double* L_16 = ___10_zi;
		double L_17 = ___4_ze;
		*((double*)L_16) = (double)L_17;
		V_2 = (7.2921159000000004E-05);
		double L_18 = ___5_vxe;
		double L_19 = V_2;
		double L_20 = ___3_ye;
		V_3 = ((double)il2cpp_codegen_subtract(L_18, ((double)il2cpp_codegen_multiply(L_19, L_20))));
		double L_21 = ___6_vye;
		double L_22 = V_2;
		double L_23 = ___2_xe;
		V_4 = ((double)il2cpp_codegen_add(L_21, ((double)il2cpp_codegen_multiply(L_22, L_23))));
		double* L_24 = ___11_vxi;
		double L_25 = V_0;
		double L_26 = V_3;
		double L_27 = V_1;
		double L_28 = V_4;
		*((double*)L_24) = (double)((double)il2cpp_codegen_subtract(((double)il2cpp_codegen_multiply(L_25, L_26)), ((double)il2cpp_codegen_multiply(L_27, L_28))));
		double* L_29 = ___12_vyi;
		double L_30 = V_1;
		double L_31 = V_3;
		double L_32 = V_0;
		double L_33 = V_4;
		*((double*)L_29) = (double)((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_30, L_31)), ((double)il2cpp_codegen_multiply(L_32, L_33))));
		double* L_34 = ___13_vzi;
		double L_35 = ___7_vze;
		*((double*)L_34) = (double)L_35;
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Geopotential_Accel_m33FE49C981409EDC26A0894E312E5AA1E0C7A176 (double ___0_x, double ___1_y, double ___2_z, DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___3_o, double ___4_mu, double ___5_re, double ___6_j2, double ___7_j3, double ___8_j4, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	double V_0 = 0.0;
	double V_1 = 0.0;
	double V_2 = 0.0;
	double V_3 = 0.0;
	double V_4 = 0.0;
	double V_5 = 0.0;
	double V_6 = 0.0;
	double V_7 = 0.0;
	double V_8 = 0.0;
	double V_9 = 0.0;
	double V_10 = 0.0;
	double V_11 = 0.0;
	double V_12 = 0.0;
	double V_13 = 0.0;
	{
		double L_0 = ___0_x;
		double L_1 = ___0_x;
		double L_2 = ___1_y;
		double L_3 = ___1_y;
		double L_4 = ___2_z;
		double L_5 = ___2_z;
		V_0 = ((double)il2cpp_codegen_add(((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_0, L_1)), ((double)il2cpp_codegen_multiply(L_2, L_3)))), ((double)il2cpp_codegen_multiply(L_4, L_5))));
		double L_6 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_7;
		L_7 = sqrt(L_6);
		V_1 = L_7;
		double L_8 = V_0;
		double L_9 = V_1;
		V_2 = ((double)il2cpp_codegen_multiply(L_8, L_9));
		double L_10 = V_0;
		double L_11 = V_2;
		V_3 = ((double)il2cpp_codegen_multiply(L_10, L_11));
		double L_12 = V_3;
		double L_13 = V_0;
		V_4 = ((double)il2cpp_codegen_multiply(L_12, L_13));
		double L_14 = ___2_z;
		double L_15 = ___2_z;
		double L_16 = V_0;
		V_5 = ((double)(((double)il2cpp_codegen_multiply(L_14, L_15))/L_16));
		double L_17 = V_5;
		double L_18 = V_5;
		V_6 = ((double)il2cpp_codegen_multiply(L_17, L_18));
		double L_19 = ___5_re;
		double L_20 = ___5_re;
		V_7 = ((double)il2cpp_codegen_multiply(L_19, L_20));
		double L_21 = V_7;
		double L_22 = ___5_re;
		V_8 = ((double)il2cpp_codegen_multiply(L_21, L_22));
		double L_23 = V_7;
		double L_24 = V_7;
		V_9 = ((double)il2cpp_codegen_multiply(L_23, L_24));
		double L_25 = ___4_mu;
		double L_26 = V_2;
		V_10 = ((double)(((-L_25))/L_26));
		double L_27 = ___4_mu;
		double L_28 = ___6_j2;
		double L_29 = V_7;
		double L_30 = V_3;
		V_11 = ((double)(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((-1.5), L_27)), L_28)), L_29))/L_30));
		double L_31 = ___4_mu;
		double L_32 = ___7_j3;
		double L_33 = V_8;
		double L_34 = V_4;
		V_12 = ((double)(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((-2.5), L_31)), L_32)), L_33))/L_34));
		double L_35 = ___4_mu;
		double L_36 = ___8_j4;
		double L_37 = V_9;
		double L_38 = V_4;
		V_13 = ((double)(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((1.875), L_35)), L_36)), L_37))/L_38));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_39 = ___3_o;
		double L_40 = V_10;
		double L_41 = ___0_x;
		double L_42 = V_11;
		double L_43 = ___0_x;
		double L_44 = V_5;
		double L_45 = V_12;
		double L_46 = ___0_x;
		double L_47 = ___2_z;
		double L_48 = V_5;
		double L_49 = V_13;
		double L_50 = ___0_x;
		double L_51 = V_5;
		double L_52 = V_6;
		NullCheck(L_39);
		(L_39)->SetAt(static_cast<il2cpp_array_size_t>(0), (double)((double)il2cpp_codegen_add(((double)il2cpp_codegen_add(((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_40, L_41)), ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_42, L_43)), ((double)il2cpp_codegen_subtract((1.0), ((double)il2cpp_codegen_multiply((5.0), L_44)))))))), ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_45, L_46)), L_47)), ((double)il2cpp_codegen_subtract((3.0), ((double)il2cpp_codegen_multiply((7.0), L_48)))))))), ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_49, L_50)), ((double)il2cpp_codegen_add(((double)il2cpp_codegen_subtract((1.0), ((double)il2cpp_codegen_multiply((14.0), L_51)))), ((double)il2cpp_codegen_multiply((21.0), L_52)))))))));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_53 = ___3_o;
		double L_54 = V_10;
		double L_55 = ___1_y;
		double L_56 = V_11;
		double L_57 = ___1_y;
		double L_58 = V_5;
		double L_59 = V_12;
		double L_60 = ___1_y;
		double L_61 = ___2_z;
		double L_62 = V_5;
		double L_63 = V_13;
		double L_64 = ___1_y;
		double L_65 = V_5;
		double L_66 = V_6;
		NullCheck(L_53);
		(L_53)->SetAt(static_cast<il2cpp_array_size_t>(1), (double)((double)il2cpp_codegen_add(((double)il2cpp_codegen_add(((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_54, L_55)), ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_56, L_57)), ((double)il2cpp_codegen_subtract((1.0), ((double)il2cpp_codegen_multiply((5.0), L_58)))))))), ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_59, L_60)), L_61)), ((double)il2cpp_codegen_subtract((3.0), ((double)il2cpp_codegen_multiply((7.0), L_62)))))))), ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_63, L_64)), ((double)il2cpp_codegen_add(((double)il2cpp_codegen_subtract((1.0), ((double)il2cpp_codegen_multiply((14.0), L_65)))), ((double)il2cpp_codegen_multiply((21.0), L_66)))))))));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_67 = ___3_o;
		double L_68 = V_10;
		double L_69 = ___2_z;
		double L_70 = V_11;
		double L_71 = ___2_z;
		double L_72 = V_5;
		double L_73 = V_12;
		double L_74 = ___2_z;
		double L_75 = ___2_z;
		double L_76 = ___2_z;
		double L_77 = ___2_z;
		double L_78 = V_5;
		double L_79 = V_0;
		double L_80 = V_13;
		double L_81 = ___2_z;
		double L_82 = V_5;
		double L_83 = V_6;
		NullCheck(L_67);
		(L_67)->SetAt(static_cast<il2cpp_array_size_t>(2), (double)((double)il2cpp_codegen_add(((double)il2cpp_codegen_add(((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_68, L_69)), ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_70, L_71)), ((double)il2cpp_codegen_subtract((3.0), ((double)il2cpp_codegen_multiply((5.0), L_72)))))))), ((double)il2cpp_codegen_multiply(L_73, ((double)il2cpp_codegen_subtract(((double)il2cpp_codegen_subtract(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((6.0), L_74)), L_75)), ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((7.0), L_76)), L_77)), L_78)))), ((double)il2cpp_codegen_multiply((0.59999999999999998), L_79)))))))), ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_80, L_81)), ((double)il2cpp_codegen_add(((double)il2cpp_codegen_subtract((5.0), ((double)il2cpp_codegen_multiply((23.333333333333332), L_82)))), ((double)il2cpp_codegen_multiply((21.0), L_83)))))))));
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR SecularRates_tD523392B109E8AC95C21E767F08383D312C0AB73 Geopotential_Rates_m3794A02018FE5E2FC72FFB20A6AF08A0C9C96103 (double ___0_a, double ___1_e, double ___2_iRad, double ___3_mu, double ___4_re, double ___5_j2, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	double V_0 = 0.0;
	double V_1 = 0.0;
	double V_2 = 0.0;
	double V_3 = 0.0;
	double V_4 = 0.0;
	SecularRates_tD523392B109E8AC95C21E767F08383D312C0AB73 V_5;
	memset((&V_5), 0, sizeof(V_5));
	{
		double L_0 = ___3_mu;
		double L_1 = ___0_a;
		double L_2 = ___0_a;
		double L_3 = ___0_a;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_4;
		L_4 = sqrt(((double)(L_0/((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_1, L_2)), L_3)))));
		V_0 = L_4;
		double L_5 = ___0_a;
		double L_6 = ___1_e;
		double L_7 = ___1_e;
		V_1 = ((double)il2cpp_codegen_multiply(L_5, ((double)il2cpp_codegen_subtract((1.0), ((double)il2cpp_codegen_multiply(L_6, L_7))))));
		double L_8 = ___2_iRad;
		double L_9;
		L_9 = cos(L_8);
		V_2 = L_9;
		double L_10 = V_2;
		double L_11 = V_2;
		V_3 = ((double)il2cpp_codegen_multiply(L_10, L_11));
		double L_12 = V_0;
		double L_13 = ___5_j2;
		double L_14 = ___4_re;
		double L_15 = V_1;
		double L_16 = ___4_re;
		double L_17 = V_1;
		V_4 = ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((1.5), L_12)), L_13)), ((double)(L_14/L_15)))), ((double)(L_16/L_17))));
		il2cpp_codegen_initobj((&V_5), sizeof(SecularRates_tD523392B109E8AC95C21E767F08383D312C0AB73));
		double L_18 = V_0;
		(&V_5)->___n = L_18;
		double L_19 = V_4;
		double L_20 = V_2;
		(&V_5)->___dOmega = ((double)il2cpp_codegen_multiply(((-L_19)), L_20));
		double L_21 = V_4;
		double L_22 = V_3;
		(&V_5)->___dArgp = ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((0.5), L_21)), ((double)il2cpp_codegen_subtract(((double)il2cpp_codegen_multiply((5.0), L_22)), (1.0)))));
		double L_23 = V_0;
		double L_24 = V_4;
		double L_25 = ___1_e;
		double L_26 = ___1_e;
		double L_27;
		L_27 = sqrt(((double)il2cpp_codegen_subtract((1.0), ((double)il2cpp_codegen_multiply(L_25, L_26)))));
		double L_28 = V_3;
		(&V_5)->___dMeanAnom = ((double)il2cpp_codegen_add(L_23, ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_24, L_27)), ((double)il2cpp_codegen_subtract(((double)il2cpp_codegen_multiply((1.5), L_28)), (0.5)))))));
		SecularRates_tD523392B109E8AC95C21E767F08383D312C0AB73 L_29 = V_5;
		return L_29;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Geopotential_NodeRateDegPerDay_m61818451245A5B3FB58FED219C189A67A385350C (double ___0_a, double ___1_e, double ___2_iDeg, const RuntimeMethod* method) 
{
	{
		double L_0 = ___0_a;
		double L_1 = ___1_e;
		double L_2 = ___2_iDeg;
		SecularRates_tD523392B109E8AC95C21E767F08383D312C0AB73 L_3;
		L_3 = Geopotential_Rates_m3794A02018FE5E2FC72FFB20A6AF08A0C9C96103(L_0, L_1, ((double)(((double)il2cpp_codegen_multiply(L_2, (3.1415926535897931)))/(180.0))), (398600441800000.0), (6378137.0), (0.00108262668), NULL);
		double L_4 = L_3.___dOmega;
		return ((double)(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_4, (86400.0))), (180.0)))/(3.1415926535897931)));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Geopotential_ArgpRateDegPerDay_mB9EA98BC597E6312BFB897EA791D41E37D7D8AB6 (double ___0_a, double ___1_e, double ___2_iDeg, const RuntimeMethod* method) 
{
	{
		double L_0 = ___0_a;
		double L_1 = ___1_e;
		double L_2 = ___2_iDeg;
		SecularRates_tD523392B109E8AC95C21E767F08383D312C0AB73 L_3;
		L_3 = Geopotential_Rates_m3794A02018FE5E2FC72FFB20A6AF08A0C9C96103(L_0, L_1, ((double)(((double)il2cpp_codegen_multiply(L_2, (3.1415926535897931)))/(180.0))), (398600441800000.0), (6378137.0), (0.00108262668), NULL);
		double L_4 = L_3.___dArgp;
		return ((double)(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_4, (86400.0))), (180.0)))/(3.1415926535897931)));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Geopotential_SsoInclinationDeg_m61C20D79D041C3EEBF0658885D6B46A4995DB011 (double ___0_a, double ___1_sunDegPerDay, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	double V_0 = 0.0;
	{
		double L_0 = ___0_a;
		double L_1 = ___0_a;
		double L_2 = ___0_a;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_3;
		L_3 = sqrt(((double)((398600441800000.0)/((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_0, L_1)), L_2)))));
		V_0 = L_3;
		double L_4 = ___1_sunDegPerDay;
		double L_5 = V_0;
		double L_6 = ___0_a;
		double L_7 = ___0_a;
		double L_8;
		L_8 = acos(((double)(((-((double)(((double)(((double)il2cpp_codegen_multiply(L_4, (3.1415926535897931)))/(180.0)))/(86400.0)))))/((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((1.5), L_5)), (0.00108262668))), ((double)((6378137.0)/L_6)))), ((double)((6378137.0)/L_7)))))));
		return ((double)(((double)il2cpp_codegen_multiply(L_8, (180.0)))/(3.1415926535897931)));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Geopotential_FrozenEccentricity_m3D6D58DD49972A331BE9C7BE3804FEAD89C7EC32 (double ___0_a, double ___1_iDeg, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		double L_0 = ___0_a;
		double L_1 = ___1_iDeg;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_2;
		L_2 = sin(((double)(((double)il2cpp_codegen_multiply(L_1, (3.1415926535897931)))/(180.0))));
		return ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((0.0011696813531327345), ((double)((6378137.0)/L_0)))), L_2));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 Geopotential_MeanToOsculating_m5BA13AA44A95567C6F4E91D146BC33CCECF9AEEE (MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 ___0_m, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	double V_0 = 0.0;
	double V_1 = 0.0;
	double V_2 = 0.0;
	double V_3 = 0.0;
	double V_4 = 0.0;
	double V_5 = 0.0;
	MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 V_6;
	memset((&V_6), 0, sizeof(V_6));
	{
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_0 = ___0_m;
		double L_1 = L_0.___a;
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_2 = ___0_m;
		double L_3 = L_2.___e;
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_4 = ___0_m;
		double L_5 = L_4.___e;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_6;
		L_6 = Math_Max_m7BAC743E1752A51F258BB82DEBDD13E7C6D3ED26(((double)il2cpp_codegen_subtract((1.0), ((double)il2cpp_codegen_multiply(L_3, L_5)))), (9.9999999999999998E-13), NULL);
		V_0 = ((double)il2cpp_codegen_multiply(L_1, L_6));
		double L_7 = V_0;
		double L_8 = V_0;
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_9 = ___0_m;
		double L_10 = L_9.___i;
		double L_11;
		L_11 = sin(L_10);
		V_1 = L_11;
		double L_12 = V_1;
		double L_13 = V_1;
		V_2 = ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((0.00108262668), ((double)((6378137.0)/L_7)))), ((double)((6378137.0)/L_8)))), ((double)il2cpp_codegen_subtract((1.0), ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((1.5), L_12)), L_13))))));
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_14 = ___0_m;
		double L_15 = L_14.___argp;
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_16 = ___0_m;
		double L_17 = L_16.___m;
		V_3 = ((double)il2cpp_codegen_add(L_15, L_17));
		double L_18 = V_3;
		double L_19;
		L_19 = cos(((double)il2cpp_codegen_multiply((2.0), L_18)));
		V_4 = L_19;
		double L_20 = V_3;
		double L_21;
		L_21 = sin(((double)il2cpp_codegen_multiply((2.0), L_20)));
		V_5 = L_21;
		il2cpp_codegen_initobj((&V_6), sizeof(MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079));
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_22 = ___0_m;
		double L_23 = L_22.___a;
		double L_24 = V_2;
		double L_25 = V_4;
		(&V_6)->___a = ((double)il2cpp_codegen_multiply(L_23, ((double)il2cpp_codegen_add((1.0), ((double)il2cpp_codegen_multiply(L_24, L_25))))));
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_26 = ___0_m;
		double L_27 = L_26.___e;
		double L_28 = V_2;
		double L_29 = V_5;
		double L_30 = V_1;
		(&V_6)->___e = ((double)il2cpp_codegen_add(L_27, ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((0.5), L_28)), L_29)), L_30))));
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_31 = ___0_m;
		double L_32 = L_31.___i;
		(&V_6)->___i = L_32;
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_33 = ___0_m;
		double L_34 = L_33.___raan;
		(&V_6)->___raan = L_34;
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_35 = ___0_m;
		double L_36 = L_35.___argp;
		(&V_6)->___argp = L_36;
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_37 = ___0_m;
		double L_38 = L_37.___m;
		(&V_6)->___m = L_38;
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_39 = V_6;
		return L_39;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 Geopotential_OsculatingToMean_mEE2B5BAF2E5B412DDE48A405E3FD1BA0331F27CC (MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 ___0_o, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	double V_0 = 0.0;
	double V_1 = 0.0;
	double V_2 = 0.0;
	double V_3 = 0.0;
	double V_4 = 0.0;
	double V_5 = 0.0;
	int32_t V_6 = 0;
	double V_7 = 0.0;
	double V_8 = 0.0;
	double V_9 = 0.0;
	double V_10 = 0.0;
	MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 V_11;
	memset((&V_11), 0, sizeof(V_11));
	int32_t G_B4_0 = 0;
	{
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_0 = ___0_o;
		double L_1 = L_0.___a;
		V_0 = L_1;
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_2 = ___0_o;
		double L_3 = L_2.___e;
		V_1 = L_3;
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_4 = ___0_o;
		double L_5 = L_4.___i;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_6;
		L_6 = sin(L_5);
		V_2 = L_6;
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_7 = ___0_o;
		double L_8 = L_7.___argp;
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_9 = ___0_o;
		double L_10 = L_9.___m;
		V_3 = ((double)il2cpp_codegen_add(L_8, L_10));
		double L_11 = V_3;
		double L_12;
		L_12 = cos(((double)il2cpp_codegen_multiply((2.0), L_11)));
		V_4 = L_12;
		double L_13 = V_3;
		double L_14;
		L_14 = sin(((double)il2cpp_codegen_multiply((2.0), L_13)));
		V_5 = L_14;
		V_6 = 0;
		goto IL_0134;
	}

IL_0054:
	{
		double L_15 = V_0;
		double L_16 = V_1;
		double L_17 = V_1;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_18;
		L_18 = Math_Max_m7BAC743E1752A51F258BB82DEBDD13E7C6D3ED26(((double)il2cpp_codegen_subtract((1.0), ((double)il2cpp_codegen_multiply(L_16, L_17)))), (9.9999999999999998E-13), NULL);
		V_7 = ((double)il2cpp_codegen_multiply(L_15, L_18));
		double L_19 = V_7;
		double L_20 = V_7;
		double L_21 = V_2;
		double L_22 = V_2;
		V_8 = ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((0.00108262668), ((double)((6378137.0)/L_19)))), ((double)((6378137.0)/L_20)))), ((double)il2cpp_codegen_subtract((1.0), ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((1.5), L_21)), L_22))))));
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_23 = ___0_o;
		double L_24 = L_23.___a;
		double L_25 = V_8;
		double L_26 = V_4;
		V_9 = ((double)(L_24/((double)il2cpp_codegen_add((1.0), ((double)il2cpp_codegen_multiply(L_25, L_26))))));
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_27 = ___0_o;
		double L_28 = L_27.___e;
		double L_29 = V_8;
		double L_30 = V_5;
		double L_31 = V_2;
		V_10 = ((double)il2cpp_codegen_subtract(L_28, ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((0.5), L_29)), L_30)), L_31))));
		double L_32 = V_9;
		double L_33 = V_0;
		double L_34;
		L_34 = fabs(((double)il2cpp_codegen_subtract(L_32, L_33)));
		double L_35 = V_9;
		double L_36;
		L_36 = fabs(L_35);
		double L_37;
		L_37 = Math_Max_m7BAC743E1752A51F258BB82DEBDD13E7C6D3ED26(L_36, (1.0), NULL);
		if ((!(((double)L_34) <= ((double)((double)il2cpp_codegen_multiply((1.0000000000000001E-15), L_37))))))
		{
			goto IL_0125;
		}
	}
	{
		double L_38 = V_10;
		double L_39 = V_1;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_40;
		L_40 = fabs(((double)il2cpp_codegen_subtract(L_38, L_39)));
		G_B4_0 = ((((int32_t)((!(((double)L_40) <= ((double)(1.0000000000000001E-15))))? 1 : 0)) == ((int32_t)0))? 1 : 0);
		goto IL_0126;
	}

IL_0125:
	{
		G_B4_0 = 0;
	}

IL_0126:
	{
		double L_41 = V_9;
		V_0 = L_41;
		double L_42 = V_10;
		V_1 = L_42;
		if (G_B4_0)
		{
			goto IL_013d;
		}
	}
	{
		int32_t L_43 = V_6;
		V_6 = ((int32_t)il2cpp_codegen_add(L_43, 1));
	}

IL_0134:
	{
		int32_t L_44 = V_6;
		if ((((int32_t)L_44) < ((int32_t)((int32_t)32))))
		{
			goto IL_0054;
		}
	}

IL_013d:
	{
		il2cpp_codegen_initobj((&V_11), sizeof(MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079));
		double L_45 = V_0;
		(&V_11)->___a = L_45;
		double L_46 = V_1;
		(&V_11)->___e = L_46;
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_47 = ___0_o;
		double L_48 = L_47.___i;
		(&V_11)->___i = L_48;
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_49 = ___0_o;
		double L_50 = L_49.___raan;
		(&V_11)->___raan = L_50;
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_51 = ___0_o;
		double L_52 = L_51.___argp;
		(&V_11)->___argp = L_52;
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_53 = ___0_o;
		double L_54 = L_53.___m;
		(&V_11)->___m = L_54;
		MeanElements_t7DA598AB638E4330804F148F23DA5C1D080C9079 L_55 = V_11;
		return L_55;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Gravity_Accel_m48DA1A5F2713BC7FE31291DD4CD2BAE64DA8EC9D (double ___0_r, double ___1_mu, const RuntimeMethod* method) 
{
	{
		double L_0 = ___1_mu;
		double L_1 = ___0_r;
		double L_2 = ___0_r;
		return ((double)(L_0/((double)il2cpp_codegen_multiply(L_1, L_2))));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Gravity_Vec_mBDF2E6E8AD1E5F3942CEFBFD735443E9B0A006D1 (double ___0_x, double ___1_y, double ___2_z, DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___3_o, double ___4_mu, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	double V_0 = 0.0;
	double V_1 = 0.0;
	double V_2 = 0.0;
	{
		double L_0 = ___0_x;
		double L_1 = ___0_x;
		double L_2 = ___1_y;
		double L_3 = ___1_y;
		double L_4 = ___2_z;
		double L_5 = ___2_z;
		V_0 = ((double)il2cpp_codegen_add(((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_0, L_1)), ((double)il2cpp_codegen_multiply(L_2, L_3)))), ((double)il2cpp_codegen_multiply(L_4, L_5))));
		double L_6 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_7;
		L_7 = sqrt(L_6);
		V_1 = L_7;
		double L_8 = ___4_mu;
		double L_9 = V_0;
		double L_10 = V_1;
		V_2 = ((double)(((-L_8))/((double)il2cpp_codegen_multiply(L_9, L_10))));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_11 = ___3_o;
		double L_12 = V_2;
		double L_13 = ___0_x;
		NullCheck(L_11);
		(L_11)->SetAt(static_cast<il2cpp_array_size_t>(0), (double)((double)il2cpp_codegen_multiply(L_12, L_13)));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_14 = ___3_o;
		double L_15 = V_2;
		double L_16 = ___1_y;
		NullCheck(L_14);
		(L_14)->SetAt(static_cast<il2cpp_array_size_t>(1), (double)((double)il2cpp_codegen_multiply(L_15, L_16)));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_17 = ___3_o;
		double L_18 = V_2;
		double L_19 = ___2_z;
		NullCheck(L_17);
		(L_17)->SetAt(static_cast<il2cpp_array_size_t>(2), (double)((double)il2cpp_codegen_multiply(L_18, L_19)));
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Earth_SiteRotationSpeed_m4408120B6BB6BCD6D9FFEEF64ADF4E24D831FE4B (double ___0_latDeg, double ___1_radius, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		double L_0 = ___1_radius;
		double L_1 = ___0_latDeg;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_2;
		L_2 = cos(((double)(((double)il2cpp_codegen_multiply(L_1, (3.1415926535897931)))/(180.0))));
		return ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((7.2921159000000004E-05), L_0)), L_2));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Earth_MinInclination_mDA899EF2B829D98CA618F44CAE427E447D63A892 (double ___0_latDeg, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		double L_0 = ___0_latDeg;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_1;
		L_1 = fabs(L_0);
		return L_1;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Handoff_PeriapsisAltitudeM_mFBB92CD52958C475187A5D94BA1A40FB8B3B8BCD (ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 ___0_el, double ___1_radius, const RuntimeMethod* method) 
{
	{
		ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 L_0 = ___0_el;
		double L_1 = L_0.___a;
		ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 L_2 = ___0_el;
		double L_3 = L_2.___e;
		double L_4 = ___1_radius;
		return ((double)il2cpp_codegen_subtract(((double)il2cpp_codegen_multiply(L_1, ((double)il2cpp_codegen_subtract((1.0), L_3)))), L_4));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Handoff_PeriapsisAltitudeM_mA0CD9CF1A3D5AB9745CFDBC849A0B60E7368CC44 (double ___0_rx, double ___1_ry, double ___2_rz, double ___3_vx, double ___4_vy, double ___5_vz, double ___6_mu, double ___7_radius, const RuntimeMethod* method) 
{
	{
		double L_0 = ___0_rx;
		double L_1 = ___1_ry;
		double L_2 = ___2_rz;
		double L_3 = ___3_vx;
		double L_4 = ___4_vy;
		double L_5 = ___5_vz;
		double L_6 = ___6_mu;
		ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 L_7;
		L_7 = Kepler_FromState_m5AC5A0E70560CCD179E939EF0787679A64967E13(L_0, L_1, L_2, L_3, L_4, L_5, L_6, NULL);
		double L_8 = ___7_radius;
		double L_9;
		L_9 = Handoff_PeriapsisAltitudeM_mFBB92CD52958C475187A5D94BA1A40FB8B3B8BCD(L_7, L_8, NULL);
		return L_9;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t Handoff_SelectCoast_mA96BEC1F85020351B41F1DDD0719B0DEFE677B3A (ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 ___0_el, const RuntimeMethod* method) 
{
	{
		ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 L_0 = ___0_el;
		double L_1;
		L_1 = Handoff_PeriapsisAltitudeM_mFBB92CD52958C475187A5D94BA1A40FB8B3B8BCD(L_0, (6378137.0), NULL);
		if ((((double)L_1) >= ((double)(600000.0))))
		{
			goto IL_001c;
		}
	}
	{
		return (int32_t)(1);
	}

IL_001c:
	{
		return (int32_t)(2);
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t Handoff_SelectCoast_m4D3BD48F7E5768155A0CDF0836328DFFF636353D (double ___0_rx, double ___1_ry, double ___2_rz, double ___3_vx, double ___4_vy, double ___5_vz, double ___6_mu, const RuntimeMethod* method) 
{
	{
		double L_0 = ___0_rx;
		double L_1 = ___1_ry;
		double L_2 = ___2_rz;
		double L_3 = ___3_vx;
		double L_4 = ___4_vy;
		double L_5 = ___5_vz;
		double L_6 = ___6_mu;
		ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 L_7;
		L_7 = Kepler_FromState_m5AC5A0E70560CCD179E939EF0787679A64967E13(L_0, L_1, L_2, L_3, L_4, L_5, L_6, NULL);
		int32_t L_8;
		L_8 = Handoff_SelectCoast_mA96BEC1F85020351B41F1DDD0719B0DEFE677B3A(L_7, NULL);
		return L_8;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 Handoff_Capture_mF1EE9012036E58770A4558E19F9D0C47FA25FFA6 (double ___0_rx, double ___1_ry, double ___2_rz, double ___3_vx, double ___4_vy, double ___5_vz, double ___6_mu, const RuntimeMethod* method) 
{
	{
		double L_0 = ___0_rx;
		double L_1 = ___1_ry;
		double L_2 = ___2_rz;
		double L_3 = ___3_vx;
		double L_4 = ___4_vy;
		double L_5 = ___5_vz;
		double L_6 = ___6_mu;
		ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 L_7;
		L_7 = Kepler_FromState_m5AC5A0E70560CCD179E939EF0787679A64967E13(L_0, L_1, L_2, L_3, L_4, L_5, L_6, NULL);
		return L_7;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Handoff_Release_m3303ED9D65AB096A6D7A2F5CEBD0C3DBDA9AD57C (ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 ___0_el, double* ___1_rx, double* ___2_ry, double* ___3_rz, double* ___4_vx, double* ___5_vy, double* ___6_vz, double ___7_mu, const RuntimeMethod* method) 
{
	{
		ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 L_0 = ___0_el;
		double* L_1 = ___1_rx;
		double* L_2 = ___2_ry;
		double* L_3 = ___3_rz;
		double* L_4 = ___4_vx;
		double* L_5 = ___5_vy;
		double* L_6 = ___6_vz;
		double L_7 = ___7_mu;
		Kepler_ToState_m90084569889091F70A4B45D0A1426F93C639792A(L_0, L_1, L_2, L_3, L_4, L_5, L_6, L_7, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Handoff_PropagateCoast_m5B341FE70E19B797AB81EC481EAA23995D38562E (double ___0_rx0, double ___1_ry0, double ___2_rz0, double ___3_vx0, double ___4_vy0, double ___5_vz0, double ___6_dt, double* ___7_rx, double* ___8_ry, double* ___9_rz, double* ___10_vx, double* ___11_vy, double* ___12_vz, Nullable_1_t3EE493957A8C36BFB8CFAD3C76358D1249FDDFA0 ___13_mode, double ___14_mu, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Nullable_1_GetValueOrDefault_mC5602D6D01DDB3D6B4DD87E77F20FA65CFFD00C4_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Nullable_1_get_HasValue_m5292804A8580CD2DD32F958EC5B17A9025B3004A_RuntimeMethod_var);
		s_Il2CppMethodInitialized = true;
	}
	ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 V_0;
	memset((&V_0), 0, sizeof(V_0));
	Nullable_1_t3EE493957A8C36BFB8CFAD3C76358D1249FDDFA0 V_1;
	memset((&V_1), 0, sizeof(V_1));
	int32_t G_B3_0 = 0;
	ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 G_B6_0;
	memset((&G_B6_0), 0, sizeof(G_B6_0));
	{
		double L_0 = ___0_rx0;
		double L_1 = ___1_ry0;
		double L_2 = ___2_rz0;
		double L_3 = ___3_vx0;
		double L_4 = ___4_vy0;
		double L_5 = ___5_vz0;
		double L_6 = ___14_mu;
		ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 L_7;
		L_7 = Kepler_FromState_m5AC5A0E70560CCD179E939EF0787679A64967E13(L_0, L_1, L_2, L_3, L_4, L_5, L_6, NULL);
		V_0 = L_7;
		Nullable_1_t3EE493957A8C36BFB8CFAD3C76358D1249FDDFA0 L_8 = ___13_mode;
		V_1 = L_8;
		bool L_9;
		L_9 = Nullable_1_get_HasValue_m5292804A8580CD2DD32F958EC5B17A9025B3004A_inline((&V_1), Nullable_1_get_HasValue_m5292804A8580CD2DD32F958EC5B17A9025B3004A_RuntimeMethod_var);
		if (L_9)
		{
			goto IL_0024;
		}
	}
	{
		ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 L_10 = V_0;
		int32_t L_11;
		L_11 = Handoff_SelectCoast_mA96BEC1F85020351B41F1DDD0719B0DEFE677B3A(L_10, NULL);
		G_B3_0 = L_11;
		goto IL_002b;
	}

IL_0024:
	{
		int32_t L_12;
		L_12 = Nullable_1_GetValueOrDefault_mC5602D6D01DDB3D6B4DD87E77F20FA65CFFD00C4_inline((&V_1), Nullable_1_GetValueOrDefault_mC5602D6D01DDB3D6B4DD87E77F20FA65CFFD00C4_RuntimeMethod_var);
		G_B3_0 = L_12;
	}

IL_002b:
	{
		if ((((int32_t)G_B3_0) == ((int32_t)2)))
		{
			goto IL_003a;
		}
	}
	{
		ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 L_13 = V_0;
		double L_14 = ___6_dt;
		double L_15 = ___14_mu;
		ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 L_16;
		L_16 = Kepler_Propagate_m898EBCF932145F43223C88DC9B1C8F5CBA58D3BA(L_13, L_14, L_15, NULL);
		G_B6_0 = L_16;
		goto IL_0044;
	}

IL_003a:
	{
		ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 L_17 = V_0;
		double L_18 = ___6_dt;
		double L_19 = ___14_mu;
		ClassicalElements_t448FE3014363130C6C2132A9ABBAABEABC22AA58 L_20;
		L_20 = OnRails_Propagate_mE026D3096F23E7BE2DEC9A9C1F486BD1789648D5(L_17, L_18, L_19, NULL);
		G_B6_0 = L_20;
	}

IL_0044:
	{
		double* L_21 = ___7_rx;
		double* L_22 = ___8_ry;
		double* L_23 = ___9_rz;
		double* L_24 = ___10_vx;
		double* L_25 = ___11_vy;
		double* L_26 = ___12_vz;
		double L_27 = ___14_mu;
		Kepler_ToState_m90084569889091F70A4B45D0A1426F93C639792A(G_B6_0, L_21, L_22, L_23, L_24, L_25, L_26, L_27, NULL);
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* SimStage_Clone_m4F9CB91D7754711D81B1D08F56A98CC361224A27 (SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		RuntimeObject* L_0;
		L_0 = Object_MemberwiseClone_m0676AEE25C3CF7C09F15ECF9EC5CC407863617B3(__this, NULL);
		return ((SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E*)CastclassSealed((RuntimeObject*)L_0, SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E_il2cpp_TypeInfo_var));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SimStage__ctor_m52F817B39D240ADCAC5E84FD5668E90C7593D591 (SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* __this, const RuntimeMethod* method) 
{
	{
		__this->___cd = (0.29999999999999999);
		Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2(__this, NULL);
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SimOptions__ctor_mB90E8E2ABA36196352F87ABFBB2D357CDD83E98E (SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* __this, const RuntimeMethod* method) 
{
	{
		__this->___targetAlt = (200000.0);
		__this->___latitude = (28.5);
		__this->___dt = (0.050000000000000003);
		__this->___tMax = (3000.0);
		__this->___kickSpeed = (60.0);
		__this->___kickAngleDeg = (4.0);
		__this->___rotate = (bool)1;
		__this->___cutoff = (bool)1;
		Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2(__this, NULL);
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* SimEvent_ToString_mA251168751A5B8358EBEEDA1C3EA6D98D10D3036 (SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Nullable_1_ToString_m5FDC0CBE068DB2893454257CE6E29846D47B3038_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Nullable_1_get_HasValue_mCF2FD8B3055FA87FC9C504F2122B3B0FAEDE3EC9_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral5040608BDCF01CCCC47DE48F5A4067A404B37F88);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral852EFDF643FA620806D7A0FC961E635054FFCC2B);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralDA39A3EE5E6B4B0D3255BFEF95601890AFD80709);
		s_Il2CppMethodInitialized = true;
	}
	Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28 V_0;
	memset((&V_0), 0, sizeof(V_0));
	String_t* G_B2_0 = NULL;
	RuntimeObject* G_B2_1 = NULL;
	String_t* G_B2_2 = NULL;
	String_t* G_B1_0 = NULL;
	RuntimeObject* G_B1_1 = NULL;
	String_t* G_B1_2 = NULL;
	String_t* G_B3_0 = NULL;
	String_t* G_B3_1 = NULL;
	RuntimeObject* G_B3_2 = NULL;
	String_t* G_B3_3 = NULL;
	{
		double L_0 = __this->___t;
		double L_1 = L_0;
		RuntimeObject* L_2 = Box(il2cpp_defaults.double_class, &L_1);
		String_t* L_3 = __this->___type;
		Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28* L_4 = (Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28*)(&__this->___stage);
		bool L_5;
		L_5 = Nullable_1_get_HasValue_mCF2FD8B3055FA87FC9C504F2122B3B0FAEDE3EC9_inline(L_4, Nullable_1_get_HasValue_mCF2FD8B3055FA87FC9C504F2122B3B0FAEDE3EC9_RuntimeMethod_var);
		if (L_5)
		{
			G_B2_0 = L_3;
			G_B2_1 = L_2;
			G_B2_2 = _stringLiteral5040608BDCF01CCCC47DE48F5A4067A404B37F88;
			goto IL_002a;
		}
		G_B1_0 = L_3;
		G_B1_1 = L_2;
		G_B1_2 = _stringLiteral5040608BDCF01CCCC47DE48F5A4067A404B37F88;
	}
	{
		G_B3_0 = _stringLiteralDA39A3EE5E6B4B0D3255BFEF95601890AFD80709;
		G_B3_1 = G_B1_0;
		G_B3_2 = G_B1_1;
		G_B3_3 = G_B1_2;
		goto IL_0048;
	}

IL_002a:
	{
		Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28 L_6 = __this->___stage;
		V_0 = L_6;
		String_t* L_7;
		L_7 = Nullable_1_ToString_m5FDC0CBE068DB2893454257CE6E29846D47B3038((&V_0), Nullable_1_ToString_m5FDC0CBE068DB2893454257CE6E29846D47B3038_RuntimeMethod_var);
		String_t* L_8;
		L_8 = String_Concat_m9E3155FB84015C823606188F53B47CB44C444991(_stringLiteral852EFDF643FA620806D7A0FC961E635054FFCC2B, L_7, NULL);
		G_B3_0 = L_8;
		G_B3_1 = G_B2_0;
		G_B3_2 = G_B2_1;
		G_B3_3 = G_B2_2;
	}

IL_0048:
	{
		String_t* L_9;
		L_9 = String_Format_mA0534D6E2AE4D67A6BD8D45B3321323930EB930C(G_B3_3, G_B3_2, G_B3_1, G_B3_0, NULL);
		return L_9;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SimEvent__ctor_m9601BFE450112DDA042CB7CB0DA82C273CCA7367 (SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* __this, const RuntimeMethod* method) 
{
	{
		Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2(__this, NULL);
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SimResult__ctor_m6EFFEB4A55446AFAE578555518F726943D7DFD4D (SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* __this, const RuntimeMethod* method) 
{
	{
		__this->___minStability = (std::numeric_limits<double>::quiet_NaN());
		__this->___minCar = (std::numeric_limits<double>::quiet_NaN());
		__this->___qLimPa = (std::numeric_limits<double>::quiet_NaN());
		__this->___coastApexAlt = (std::numeric_limits<double>::quiet_NaN());
		Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2(__this, NULL);
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* Headless_Normalize_m0BE85272851F10E3AF5FC646F5218F7C394DAE14 (RuntimeObject* ___0_stages, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&ICollection_1_t88F05726DAA7C0AE3C0938257EA5DEF9546C2B94_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IList_1_t90735131CC8058817BB0091BD9AB0D05090C0A58_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral1F60DDB24B17EAD7ECD8D55CDEA85F2C9C741B97);
		s_Il2CppMethodInitialized = true;
	}
	SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* V_0 = NULL;
	int32_t V_1 = 0;
	SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* V_2 = NULL;
	double V_3 = 0.0;
	double V_4 = 0.0;
	SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* G_B3_0 = NULL;
	SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* G_B2_0 = NULL;
	SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* G_B4_0 = NULL;
	String_t* G_B5_0 = NULL;
	SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* G_B5_1 = NULL;
	{
		RuntimeObject* L_0 = ___0_stages;
		NullCheck(L_0);
		int32_t L_1;
		L_1 = InterfaceFuncInvoker0< int32_t >::Invoke(0, ICollection_1_t88F05726DAA7C0AE3C0938257EA5DEF9546C2B94_il2cpp_TypeInfo_var, L_0);
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_2 = (SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641*)(SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641*)SZArrayNew(SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641_il2cpp_TypeInfo_var, (uint32_t)L_1);
		V_0 = L_2;
		V_1 = 0;
		goto IL_00d7;
	}

IL_0013:
	{
		RuntimeObject* L_3 = ___0_stages;
		int32_t L_4 = V_1;
		NullCheck(L_3);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_5;
		L_5 = InterfaceFuncInvoker1< SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E*, int32_t >::Invoke(0, IList_1_t90735131CC8058817BB0091BD9AB0D05090C0A58_il2cpp_TypeInfo_var, L_3, L_4);
		NullCheck(L_5);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_6;
		L_6 = SimStage_Clone_m4F9CB91D7754711D81B1D08F56A98CC361224A27(L_5, NULL);
		V_2 = L_6;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_7 = V_2;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_8 = V_2;
		NullCheck(L_8);
		double L_9 = L_8->___thrustVac;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_10 = V_2;
		NullCheck(L_10);
		double L_11 = L_10->___ispVac;
		double L_12;
		L_12 = RocketEq_MassFlow_mB82582EAD2B0F84192C93DBF5DD78465988C5834(L_9, L_11, NULL);
		NullCheck(L_7);
		L_7->___mdot = L_12;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_13 = V_2;
		NullCheck(L_13);
		double L_14 = L_13->___mdot;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_15 = V_2;
		NullCheck(L_15);
		double L_16 = L_15->___ispSL;
		V_3 = ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_14, (9.8066499999999994))), L_16));
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_17 = V_2;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_18 = V_2;
		NullCheck(L_18);
		double L_19 = L_18->___thrustSL;
		if ((!(((double)L_19) > ((double)(0.0)))))
		{
			G_B3_0 = L_17;
			goto IL_0080;
		}
		G_B2_0 = L_17;
	}
	{
		double L_20 = V_3;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_21 = V_2;
		NullCheck(L_21);
		double L_22 = L_21->___thrustSL;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_23;
		L_23 = fabs(((double)il2cpp_codegen_subtract(L_20, L_22)));
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_24 = V_2;
		NullCheck(L_24);
		double L_25 = L_24->___thrustSL;
		if ((((double)((double)(L_23/L_25))) > ((double)(0.050000000000000003))))
		{
			G_B4_0 = G_B2_0;
			goto IL_0083;
		}
		G_B3_0 = G_B2_0;
	}

IL_0080:
	{
		G_B5_0 = ((String_t*)(NULL));
		G_B5_1 = G_B3_0;
		goto IL_00b9;
	}

IL_0083:
	{
		int32_t L_26 = V_1;
		int32_t L_27 = ((int32_t)il2cpp_codegen_add(L_26, 1));
		RuntimeObject* L_28 = Box(il2cpp_defaults.int32_class, &L_27);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_29 = V_2;
		NullCheck(L_29);
		double* L_30 = (double*)(&L_29->___thrustSL);
		il2cpp_codegen_runtime_class_init_inline(CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0_il2cpp_TypeInfo_var);
		CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0* L_31;
		L_31 = CultureInfo_get_InvariantCulture_mD1E96DC845E34B10F78CB744B0CB5D7D63CEB1E6(NULL);
		String_t* L_32;
		L_32 = Double_ToString_m4318830D9F771852FDCF21C14CF9E8ABC7E77357(L_30, L_31, NULL);
		double L_33 = V_3;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_34;
		L_34 = bankers_round(L_33);
		V_4 = L_34;
		CultureInfo_t9BA817D41AD55AC8BD07480DD8AC22F8FFA378E0* L_35;
		L_35 = CultureInfo_get_InvariantCulture_mD1E96DC845E34B10F78CB744B0CB5D7D63CEB1E6(NULL);
		String_t* L_36;
		L_36 = Double_ToString_m4318830D9F771852FDCF21C14CF9E8ABC7E77357((&V_4), L_35, NULL);
		String_t* L_37;
		L_37 = String_Format_mA0534D6E2AE4D67A6BD8D45B3321323930EB930C(_stringLiteral1F60DDB24B17EAD7ECD8D55CDEA85F2C9C741B97, L_28, L_32, L_36, NULL);
		G_B5_0 = L_37;
		G_B5_1 = G_B4_0;
	}

IL_00b9:
	{
		NullCheck(G_B5_1);
		G_B5_1->___warn = G_B5_0;
		Il2CppCodeGenWriteBarrier((void**)(&G_B5_1->___warn), (void*)G_B5_0);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_38 = V_2;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_39 = V_2;
		NullCheck(L_39);
		double L_40 = L_39->___diameter;
		double L_41;
		L_41 = Aero_RefArea_m98DD153C9A01FEFA95889E85B5CB928AA579A457(L_40, NULL);
		NullCheck(L_38);
		L_38->___area = L_41;
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_42 = V_0;
		int32_t L_43 = V_1;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_44 = V_2;
		NullCheck(L_42);
		(L_42)->SetAt(static_cast<il2cpp_array_size_t>(L_43), (SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E*)L_44);
		int32_t L_45 = V_1;
		V_1 = ((int32_t)il2cpp_codegen_add(L_45, 1));
	}

IL_00d7:
	{
		int32_t L_46 = V_1;
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_47 = V_0;
		NullCheck(L_47);
		if ((((int32_t)L_46) < ((int32_t)((int32_t)(((RuntimeArray*)L_47)->max_length)))))
		{
			goto IL_0013;
		}
	}
	{
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_48 = V_0;
		return L_48;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ValueTuple_2_tCD83B33C76656F32DCB440F5228F21BF25762DE2 Headless_StackDeltaV_m9C51D98CCE8BDDE9B7F7F6014E1947219FC73C3C (RuntimeObject* ___0_stages, double ___1_payload, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&ValueTuple_2__ctor_m945C088412D024EDD72CDA156A7A63BB5E0D0688_RuntimeMethod_var);
		s_Il2CppMethodInitialized = true;
	}
	SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* V_0 = NULL;
	DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* V_1 = NULL;
	double V_2 = 0.0;
	int32_t V_3 = 0;
	double V_4 = 0.0;
	double V_5 = 0.0;
	int32_t V_6 = 0;
	DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* V_7 = NULL;
	int32_t V_8 = 0;
	double V_9 = 0.0;
	{
		RuntimeObject* L_0 = ___0_stages;
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_1;
		L_1 = Headless_Normalize_m0BE85272851F10E3AF5FC646F5218F7C394DAE14(L_0, NULL);
		V_0 = L_1;
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_2 = V_0;
		NullCheck(L_2);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_3 = (DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE*)(DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE*)SZArrayNew(DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE_il2cpp_TypeInfo_var, (uint32_t)((int32_t)(((RuntimeArray*)L_2)->max_length)));
		V_1 = L_3;
		V_2 = (0.0);
		V_3 = 0;
		goto IL_0084;
	}

IL_001e:
	{
		double L_4 = ___1_payload;
		V_4 = L_4;
		int32_t L_5 = V_3;
		V_6 = ((int32_t)il2cpp_codegen_add(L_5, 1));
		goto IL_0046;
	}

IL_0028:
	{
		double L_6 = V_4;
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_7 = V_0;
		int32_t L_8 = V_6;
		NullCheck(L_7);
		int32_t L_9 = L_8;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_10 = (L_7)->GetAt(static_cast<il2cpp_array_size_t>(L_9));
		NullCheck(L_10);
		double L_11 = L_10->___propMass;
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_12 = V_0;
		int32_t L_13 = V_6;
		NullCheck(L_12);
		int32_t L_14 = L_13;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_15 = (L_12)->GetAt(static_cast<il2cpp_array_size_t>(L_14));
		NullCheck(L_15);
		double L_16 = L_15->___dryMass;
		V_4 = ((double)il2cpp_codegen_add(L_6, ((double)il2cpp_codegen_add(L_11, L_16))));
		int32_t L_17 = V_6;
		V_6 = ((int32_t)il2cpp_codegen_add(L_17, 1));
	}

IL_0046:
	{
		int32_t L_18 = V_6;
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_19 = V_0;
		NullCheck(L_19);
		if ((((int32_t)L_18) < ((int32_t)((int32_t)(((RuntimeArray*)L_19)->max_length)))))
		{
			goto IL_0028;
		}
	}
	{
		double L_20 = V_4;
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_21 = V_0;
		int32_t L_22 = V_3;
		NullCheck(L_21);
		int32_t L_23 = L_22;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_24 = (L_21)->GetAt(static_cast<il2cpp_array_size_t>(L_23));
		NullCheck(L_24);
		double L_25 = L_24->___propMass;
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_26 = V_0;
		int32_t L_27 = V_3;
		NullCheck(L_26);
		int32_t L_28 = L_27;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_29 = (L_26)->GetAt(static_cast<il2cpp_array_size_t>(L_28));
		NullCheck(L_29);
		double L_30 = L_29->___dryMass;
		V_5 = ((double)il2cpp_codegen_add(((double)il2cpp_codegen_add(L_20, L_25)), L_30));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_31 = V_1;
		int32_t L_32 = V_3;
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_33 = V_0;
		int32_t L_34 = V_3;
		NullCheck(L_33);
		int32_t L_35 = L_34;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_36 = (L_33)->GetAt(static_cast<il2cpp_array_size_t>(L_35));
		NullCheck(L_36);
		double L_37 = L_36->___ispVac;
		double L_38 = V_5;
		double L_39 = V_5;
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_40 = V_0;
		int32_t L_41 = V_3;
		NullCheck(L_40);
		int32_t L_42 = L_41;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_43 = (L_40)->GetAt(static_cast<il2cpp_array_size_t>(L_42));
		NullCheck(L_43);
		double L_44 = L_43->___propMass;
		double L_45;
		L_45 = RocketEq_DeltaV_m2BD275655078E8767D0994CE4B961CB5D11AB488(L_37, L_38, ((double)il2cpp_codegen_subtract(L_39, L_44)), NULL);
		NullCheck(L_31);
		(L_31)->SetAt(static_cast<il2cpp_array_size_t>(L_32), (double)L_45);
		int32_t L_46 = V_3;
		V_3 = ((int32_t)il2cpp_codegen_add(L_46, 1));
	}

IL_0084:
	{
		int32_t L_47 = V_3;
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_48 = V_0;
		NullCheck(L_48);
		if ((((int32_t)L_47) < ((int32_t)((int32_t)(((RuntimeArray*)L_48)->max_length)))))
		{
			goto IL_001e;
		}
	}
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_49 = V_1;
		V_7 = L_49;
		V_8 = 0;
		goto IL_00a4;
	}

IL_0092:
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_50 = V_7;
		int32_t L_51 = V_8;
		NullCheck(L_50);
		int32_t L_52 = L_51;
		double L_53 = (L_50)->GetAt(static_cast<il2cpp_array_size_t>(L_52));
		V_9 = L_53;
		double L_54 = V_2;
		double L_55 = V_9;
		V_2 = ((double)il2cpp_codegen_add(L_54, L_55));
		int32_t L_56 = V_8;
		V_8 = ((int32_t)il2cpp_codegen_add(L_56, 1));
	}

IL_00a4:
	{
		int32_t L_57 = V_8;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_58 = V_7;
		NullCheck(L_58);
		if ((((int32_t)L_57) < ((int32_t)((int32_t)(((RuntimeArray*)L_58)->max_length)))))
		{
			goto IL_0092;
		}
	}
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_59 = V_1;
		double L_60 = V_2;
		ValueTuple_2_tCD83B33C76656F32DCB440F5228F21BF25762DE2 L_61;
		memset((&L_61), 0, sizeof(L_61));
		ValueTuple_2__ctor_m945C088412D024EDD72CDA156A7A63BB5E0D0688((&L_61), L_59, L_60, ValueTuple_2__ctor_m945C088412D024EDD72CDA156A7A63BB5E0D0688_RuntimeMethod_var);
		return L_61;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* Headless_Simulate_m38DD1AFE775A7EA4CC7417F422829F4DB69ED93D (RuntimeObject* ___0_stages, SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* ___1_opts, RuntimeObject* ___2_sink, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* V_0 = NULL;
	SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* G_B2_0 = NULL;
	RuntimeObject* G_B2_1 = NULL;
	SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* G_B1_0 = NULL;
	RuntimeObject* G_B1_1 = NULL;
	{
		RuntimeObject* L_0 = ___0_stages;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_1 = ___1_opts;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_2 = L_1;
		if (L_2)
		{
			G_B2_0 = L_2;
			G_B2_1 = L_0;
			goto IL_000b;
		}
		G_B1_0 = L_2;
		G_B1_1 = L_0;
	}
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_3 = (SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B*)il2cpp_codegen_object_new(SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B_il2cpp_TypeInfo_var);
		SimOptions__ctor_mB90E8E2ABA36196352F87ABFBB2D357CDD83E98E(L_3, NULL);
		G_B2_0 = L_3;
		G_B2_1 = G_B1_1;
	}

IL_000b:
	{
		AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* L_4 = (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C*)il2cpp_codegen_object_new(AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C_il2cpp_TypeInfo_var);
		AscentSim__ctor_m817E2F94B05477E9AC311A810BB1700205867B9A(L_4, G_B2_1, G_B2_0, NULL);
		V_0 = L_4;
		AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* L_5 = V_0;
		RuntimeObject* L_6 = ___2_sink;
		NullCheck(L_5);
		AscentSim_set_Sink_m83A341CB52EE9FD70B2BF88750BE6F611CC75295_inline(L_5, L_6, NULL);
	}

IL_0018:
	{
		AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* L_7 = V_0;
		NullCheck(L_7);
		bool L_8;
		L_8 = AscentSim_Tick_m3AE54AD6763AE540AEC122CE9F9AF31974596B28(L_7, NULL);
		if (L_8)
		{
			goto IL_0018;
		}
	}
	{
		AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* L_9 = V_0;
		NullCheck(L_9);
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_10;
		L_10 = AscentSim_Finish_m38F11D0801DA64CA9E5EECC1BCB1C1F6A720A6D0(L_9, NULL);
		return L_10;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Headless_LegacyPitch_m7901412AEEAE5BE07A9992D2FD9FF0D30EADE09C (GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307* ___0_g, const RuntimeMethod* method) 
{
	{
		GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307* L_0 = ___0_g;
		if (L_0)
		{
			goto IL_0005;
		}
	}
	{
		return (bool)1;
	}

IL_0005:
	{
		GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307* L_1 = ___0_g;
		NullCheck(L_1);
		bool L_2 = L_1->___useAltitudeKick;
		if (L_2)
		{
			goto IL_0015;
		}
	}
	{
		GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307* L_3 = ___0_g;
		NullCheck(L_3);
		bool L_4 = L_3->___turnShapeSet;
		if (!L_4)
		{
			goto IL_0017;
		}
	}

IL_0015:
	{
		return (bool)0;
	}

IL_0017:
	{
		GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307* L_5 = ___0_g;
		NullCheck(L_5);
		double L_6 = L_5->___pitchKickDeg;
		return (bool)((((double)L_6) == ((double)(4.0)))? 1 : 0);
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* Headless_MediumLifter_mCFBCEA9109B2E7F5DE8A11C11495BCCDCA00252B (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_0 = (SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641*)(SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641*)SZArrayNew(SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641_il2cpp_TypeInfo_var, (uint32_t)2);
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_1 = L_0;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_2 = (SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E*)il2cpp_codegen_object_new(SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E_il2cpp_TypeInfo_var);
		SimStage__ctor_m52F817B39D240ADCAC5E84FD5668E90C7593D591(L_2, NULL);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_3 = L_2;
		NullCheck(L_3);
		L_3->___thrustSL = (7600000.0);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_4 = L_3;
		NullCheck(L_4);
		L_4->___thrustVac = (8230000.0);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_5 = L_4;
		NullCheck(L_5);
		L_5->___ispSL = (282.0);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_6 = L_5;
		NullCheck(L_6);
		L_6->___ispVac = (311.0);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_7 = L_6;
		NullCheck(L_7);
		L_7->___propMass = (411000.0);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_8 = L_7;
		NullCheck(L_8);
		L_8->___dryMass = (25600.0);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_9 = L_8;
		NullCheck(L_9);
		L_9->___diameter = (3.7000000000000002);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_10 = L_9;
		NullCheck(L_10);
		L_10->___cd = (0.29999999999999999);
		NullCheck(L_1);
		(L_1)->SetAt(static_cast<il2cpp_array_size_t>(0), (SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E*)L_10);
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_11 = L_1;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_12 = (SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E*)il2cpp_codegen_object_new(SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E_il2cpp_TypeInfo_var);
		SimStage__ctor_m52F817B39D240ADCAC5E84FD5668E90C7593D591(L_12, NULL);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_13 = L_12;
		NullCheck(L_13);
		L_13->___thrustSL = (0.0);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_14 = L_13;
		NullCheck(L_14);
		L_14->___thrustVac = (981000.0);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_15 = L_14;
		NullCheck(L_15);
		L_15->___ispSL = (200.0);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_16 = L_15;
		NullCheck(L_16);
		L_16->___ispVac = (348.0);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_17 = L_16;
		NullCheck(L_17);
		L_17->___propMass = (107500.0);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_18 = L_17;
		NullCheck(L_18);
		L_18->___dryMass = (4500.0);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_19 = L_18;
		NullCheck(L_19);
		L_19->___diameter = (3.7000000000000002);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_20 = L_19;
		NullCheck(L_20);
		L_20->___cd = (0.29999999999999999);
		NullCheck(L_11);
		(L_11)->SetAt(static_cast<il2cpp_array_size_t>(1), (SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E*)L_20);
		return L_11;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___U3CStageIdxU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void AscentSim_set_StageIdx_m0D20B990A04C0A885ABBD3A3B4861165D5091CAF (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, int32_t ___0_value, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = ___0_value;
		__this->___U3CStageIdxU3Ek__BackingField = L_0;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t AscentSim_get_Mode_m5522E937FEF440F09E1F28A50A534066FEA2E2C9 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___U3CModeU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void AscentSim_set_Mode_m7946FC27FD607C87B37DB52807785705728CFDC3 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, int32_t ___0_value, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = ___0_value;
		__this->___U3CModeU3Ek__BackingField = L_0;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double AscentSim_get_PitchCmd_mC76BA7E009D9C12D9F966E247CC7909311A135ED (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		double L_0 = __this->___U3CPitchCmdU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void AscentSim_set_PitchCmd_m54C6E65BF6705E8F4DD4DC357074BD0DF8CA677A (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, double ___0_value, const RuntimeMethod* method) 
{
	{
		double L_0 = ___0_value;
		__this->___U3CPitchCmdU3Ek__BackingField = L_0;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool AscentSim_get_Thrusting_m1221CE066B159B7647B0988CCA625853DB547A22 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		bool L_0 = __this->___U3CThrustingU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void AscentSim_set_Thrusting_mD9D054E78DF826C6D731FFE51269A92EBD1D3992 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, bool ___0_value, const RuntimeMethod* method) 
{
	{
		bool L_0 = ___0_value;
		__this->___U3CThrustingU3Ek__BackingField = L_0;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		double L_0 = __this->___U3CTU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void AscentSim_set_T_mECCDAC4D95A8329E97710AAC1A222166080AE6F5 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, double ___0_value, const RuntimeMethod* method) 
{
	{
		double L_0 = ___0_value;
		__this->___U3CTU3Ek__BackingField = L_0;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool AscentSim_get_Done_mBC8EEA714EDD0073C9F4D91135D515D022B41A6A (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		bool L_0 = __this->___U3CDoneU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void AscentSim_set_Done_m95D7993A7D005264297DDDDF34649C09515E702C (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, bool ___0_value, const RuntimeMethod* method) 
{
	{
		bool L_0 = ___0_value;
		__this->___U3CDoneU3Ek__BackingField = L_0;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t AscentSim_get_StageCount_mFCF30E128AD8BC5D16A61DE3D22F30B157DF153A (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_0 = __this->___st;
		NullCheck(L_0);
		return ((int32_t)(((RuntimeArray*)L_0)->max_length));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double AscentSim_get_LossGravity_mA4E948D7AF4443C947A98500CA41C1678FB1FEFA (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		double L_0 = __this->___lossG;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double AscentSim_get_LossDrag_m59321AF106CF9F6F70FD422380790FAB4D0CA43A (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		double L_0 = __this->___lossD;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double AscentSim_get_LossSteering_m6BE283FD84A8088703AFF4D8073F57D74FEF027F (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		double L_0 = __this->___lossS;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* AscentSim_get_Sink_m34F4D7A8103A12F8CECC0E1F780FA136EC04C37A (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		RuntimeObject* L_0 = __this->___U3CSinkU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void AscentSim_set_Sink_m83A341CB52EE9FD70B2BF88750BE6F611CC75295 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, RuntimeObject* ___0_value, const RuntimeMethod* method) 
{
	{
		RuntimeObject* L_0 = ___0_value;
		__this->___U3CSinkU3Ek__BackingField = L_0;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___U3CSinkU3Ek__BackingField), (void*)L_0);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double AscentSim_get_PropellantFraction_mF2BD0FA53A320EE2E84DF86342722250A23EC4DC (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	double V_0 = 0.0;
	{
		bool L_0;
		L_0 = AscentSim_get_Thrusting_m1221CE066B159B7647B0988CCA625853DB547A22_inline(__this, NULL);
		if (!L_0)
		{
			goto IL_003e;
		}
	}
	{
		int32_t L_1;
		L_1 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		if ((((int32_t)L_1) < ((int32_t)0)))
		{
			goto IL_003e;
		}
	}
	{
		int32_t L_2;
		L_2 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_3 = __this->___st;
		NullCheck(L_3);
		if ((((int32_t)L_2) >= ((int32_t)((int32_t)(((RuntimeArray*)L_3)->max_length)))))
		{
			goto IL_003e;
		}
	}
	{
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_4 = __this->___st;
		int32_t L_5;
		L_5 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		NullCheck(L_4);
		int32_t L_6 = L_5;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_7 = (L_4)->GetAt(static_cast<il2cpp_array_size_t>(L_6));
		NullCheck(L_7);
		double L_8 = L_7->___propMass;
		if ((((double)L_8) > ((double)(0.0))))
		{
			goto IL_0048;
		}
	}

IL_003e:
	{
		return (std::numeric_limits<double>::quiet_NaN());
	}

IL_0048:
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_9 = __this->___Y;
		NullCheck(L_9);
		int32_t L_10 = 4;
		double L_11 = (L_9)->GetAt(static_cast<il2cpp_array_size_t>(L_10));
		double L_12;
		L_12 = AscentSim_StageEndMass_mB2D815CE13FEE7A63EAA69082D706CB616D6C623(__this, NULL);
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_13 = __this->___st;
		int32_t L_14;
		L_14 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		NullCheck(L_13);
		int32_t L_15 = L_14;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_16 = (L_13)->GetAt(static_cast<il2cpp_array_size_t>(L_15));
		NullCheck(L_16);
		double L_17 = L_16->___propMass;
		V_0 = ((double)(((double)il2cpp_codegen_subtract(L_11, L_12))/L_17));
		double L_18 = V_0;
		bool L_19;
		L_19 = Double_IsNaN_mF2BC6D1FD4813179B2CAE58D29770E42830D0883_inline(L_18, NULL);
		if (L_19)
		{
			goto IL_007f;
		}
	}
	{
		double L_20 = V_0;
		if ((!(((double)L_20) < ((double)(0.0)))))
		{
			goto IL_0089;
		}
	}

IL_007f:
	{
		return (0.0);
	}

IL_0089:
	{
		double L_21 = V_0;
		if ((!(((double)L_21) > ((double)(1.0)))))
		{
			goto IL_009f;
		}
	}
	{
		return (1.0);
	}

IL_009f:
	{
		double L_22 = V_0;
		return L_22;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void AscentSim__ctor_m817E2F94B05477E9AC311A810BB1700205867B9A (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, RuntimeObject* ___0_stages, SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* ___1_opts, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&AscentSim_Deriv_m4147891C17132CF460ABF67F65FA78578EE2235A_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Deriv_t32B789F19EA5F7301FEE48A702C2B32AF64EF69A_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1__ctor_m17F0A7DA25FD5C6456AA73F6E13F0869DB851C82_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1__ctor_mC776F2E1B0BF7A914DD5F7B2DB3E7B333D09A513_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Nullable_1__ctor_m141FA88563AC0B5179132FB929EABD02C47FF703_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&RK4_t1BC6735398DED08AF0675CEEB7983FDA7AB140CB_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral4044F71E1BE65840634E2AF1C09536F10DBF2AF5);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralC22AD2B06041300DA8ABAAA301A2E02B285863B3);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* G_B5_0 = NULL;
	AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* G_B4_0 = NULL;
	double G_B6_0 = 0.0;
	AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* G_B6_1 = NULL;
	AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* G_B8_0 = NULL;
	AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* G_B7_0 = NULL;
	double G_B9_0 = 0.0;
	AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* G_B9_1 = NULL;
	{
		RK4_t1BC6735398DED08AF0675CEEB7983FDA7AB140CB* L_0 = (RK4_t1BC6735398DED08AF0675CEEB7983FDA7AB140CB*)il2cpp_codegen_object_new(RK4_t1BC6735398DED08AF0675CEEB7983FDA7AB140CB_il2cpp_TypeInfo_var);
		RK4__ctor_mC4686CE4A404E704347D2236C13F1BAA316F56DC(L_0, 5, NULL);
		__this->___rk = L_0;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___rk), (void*)L_0);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_1 = (DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE*)(DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE*)SZArrayNew(DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE_il2cpp_TypeInfo_var, (uint32_t)5);
		__this->___Y = L_1;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___Y), (void*)L_1);
		__this->___U3CPitchCmdU3Ek__BackingField = (1.5707963267948966);
		__this->___U3CThrustingU3Ek__BackingField = (bool)1;
		List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5* L_2 = (List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5*)il2cpp_codegen_object_new(List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5_il2cpp_TypeInfo_var);
		List_1__ctor_m17F0A7DA25FD5C6456AA73F6E13F0869DB851C82(L_2, List_1__ctor_m17F0A7DA25FD5C6456AA73F6E13F0869DB851C82_RuntimeMethod_var);
		__this->___Events = L_2;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___Events), (void*)L_2);
		List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8* L_3 = (List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8*)il2cpp_codegen_object_new(List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8_il2cpp_TypeInfo_var);
		List_1__ctor_mC776F2E1B0BF7A914DD5F7B2DB3E7B333D09A513(L_3, List_1__ctor_mC776F2E1B0BF7A914DD5F7B2DB3E7B333D09A513_RuntimeMethod_var);
		__this->___Samples = L_3;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___Samples), (void*)L_3);
		__this->___kickStart = (-1.0);
		__this->___thrustScale = (1.0);
		__this->___outcome = _stringLiteral4044F71E1BE65840634E2AF1C09536F10DBF2AF5;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___outcome), (void*)_stringLiteral4044F71E1BE65840634E2AF1C09536F10DBF2AF5);
		__this->___LiveThrottleScale = (1.0);
		Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2(__this, NULL);
		RuntimeObject* L_4 = ___0_stages;
		__this->___stagesIn = L_4;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___stagesIn), (void*)L_4);
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_5 = ___1_opts;
		__this->___o = L_5;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___o), (void*)L_5);
		RuntimeObject* L_6 = ___0_stages;
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_7;
		L_7 = Headless_Normalize_m0BE85272851F10E3AF5FC646F5218F7C394DAE14(L_6, NULL);
		__this->___st = L_7;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___st), (void*)L_7);
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_8 = __this->___o;
		NullCheck(L_8);
		double L_9 = L_8->___kickAngleDeg;
		__this->___kickAngle = ((double)(((double)il2cpp_codegen_multiply(L_9, (3.1415926535897931)))/(180.0)));
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_10 = __this->___st;
		NullCheck(L_10);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_11 = (DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE*)(DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE*)SZArrayNew(DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE_il2cpp_TypeInfo_var, (uint32_t)((int32_t)il2cpp_codegen_add(((int32_t)(((RuntimeArray*)L_10)->max_length)), 1)));
		__this->___upperMass = L_11;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___upperMass), (void*)L_11);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_12 = __this->___upperMass;
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_13 = __this->___st;
		NullCheck(L_13);
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_14 = __this->___o;
		NullCheck(L_14);
		double L_15 = L_14->___payload;
		NullCheck(L_12);
		(L_12)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)(((RuntimeArray*)L_13)->max_length))), (double)L_15);
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_16 = __this->___st;
		NullCheck(L_16);
		V_0 = ((int32_t)il2cpp_codegen_subtract(((int32_t)(((RuntimeArray*)L_16)->max_length)), 1));
		goto IL_012f;
	}

IL_00fd:
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_17 = __this->___upperMass;
		int32_t L_18 = V_0;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_19 = __this->___upperMass;
		int32_t L_20 = V_0;
		NullCheck(L_19);
		int32_t L_21 = ((int32_t)il2cpp_codegen_add(L_20, 1));
		double L_22 = (L_19)->GetAt(static_cast<il2cpp_array_size_t>(L_21));
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_23 = __this->___st;
		int32_t L_24 = V_0;
		NullCheck(L_23);
		int32_t L_25 = L_24;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_26 = (L_23)->GetAt(static_cast<il2cpp_array_size_t>(L_25));
		NullCheck(L_26);
		double L_27 = L_26->___propMass;
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_28 = __this->___st;
		int32_t L_29 = V_0;
		NullCheck(L_28);
		int32_t L_30 = L_29;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_31 = (L_28)->GetAt(static_cast<il2cpp_array_size_t>(L_30));
		NullCheck(L_31);
		double L_32 = L_31->___dryMass;
		NullCheck(L_17);
		(L_17)->SetAt(static_cast<il2cpp_array_size_t>(L_18), (double)((double)il2cpp_codegen_add(((double)il2cpp_codegen_add(L_22, L_27)), L_32)));
		int32_t L_33 = V_0;
		V_0 = ((int32_t)il2cpp_codegen_subtract(L_33, 1));
	}

IL_012f:
	{
		int32_t L_34 = V_0;
		if ((((int32_t)L_34) >= ((int32_t)0)))
		{
			goto IL_00fd;
		}
	}
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_35 = __this->___o;
		NullCheck(L_35);
		bool L_36 = L_35->___rotate;
		if (L_36)
		{
			G_B5_0 = __this;
			goto IL_014c;
		}
		G_B4_0 = __this;
	}
	{
		G_B6_0 = (0.0);
		G_B6_1 = G_B4_0;
		goto IL_0165;
	}

IL_014c:
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_37 = __this->___o;
		NullCheck(L_37);
		double L_38 = L_37->___latitude;
		double L_39;
		L_39 = Earth_SiteRotationSpeed_m4408120B6BB6BCD6D9FFEEF64ADF4E24D831FE4B(L_38, (6378137.0), NULL);
		G_B6_0 = L_39;
		G_B6_1 = G_B5_0;
	}

IL_0165:
	{
		NullCheck(G_B6_1);
		G_B6_1->___vSite = G_B6_0;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_40 = __this->___Y;
		NullCheck(L_40);
		(L_40)->SetAt(static_cast<il2cpp_array_size_t>(0), (double)(0.0));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_41 = __this->___Y;
		NullCheck(L_41);
		(L_41)->SetAt(static_cast<il2cpp_array_size_t>(1), (double)(6378137.0));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_42 = __this->___Y;
		double L_43 = __this->___vSite;
		NullCheck(L_42);
		(L_42)->SetAt(static_cast<il2cpp_array_size_t>(2), (double)((-L_43)));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_44 = __this->___Y;
		NullCheck(L_44);
		(L_44)->SetAt(static_cast<il2cpp_array_size_t>(3), (double)(0.0));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_45 = __this->___Y;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_46 = __this->___upperMass;
		NullCheck(L_46);
		int32_t L_47 = 0;
		double L_48 = (L_46)->GetAt(static_cast<il2cpp_array_size_t>(L_47));
		NullCheck(L_45);
		(L_45)->SetAt(static_cast<il2cpp_array_size_t>(4), (double)L_48);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_49 = __this->___Y;
		NullCheck(L_49);
		int32_t L_50 = 0;
		double L_51 = (L_49)->GetAt(static_cast<il2cpp_array_size_t>(L_50));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_52 = __this->___Y;
		NullCheck(L_52);
		int32_t L_53 = 1;
		double L_54 = (L_52)->GetAt(static_cast<il2cpp_array_size_t>(L_53));
		FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 L_55;
		memset((&L_55), 0, sizeof(L_55));
		FlightVec__ctor_mCE371A7462C9E8DD33BDB342A3CDF34D1750CFA4((&L_55), L_51, L_54, NULL);
		__this->___pad = L_55;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_56 = __this->___o;
		NullCheck(L_56);
		bool L_57 = L_56->___rotate;
		if (L_57)
		{
			G_B8_0 = __this;
			goto IL_01f0;
		}
		G_B7_0 = __this;
	}
	{
		G_B9_0 = (0.0);
		G_B9_1 = G_B7_0;
		goto IL_0200;
	}

IL_01f0:
	{
		double L_58 = __this->___vSite;
		G_B9_0 = ((double)(L_58/(6378137.0)));
		G_B9_1 = G_B8_0;
	}

IL_0200:
	{
		NullCheck(G_B9_1);
		G_B9_1->___omegaAir = G_B9_0;
		Deriv_t32B789F19EA5F7301FEE48A702C2B32AF64EF69A* L_59 = (Deriv_t32B789F19EA5F7301FEE48A702C2B32AF64EF69A*)il2cpp_codegen_object_new(Deriv_t32B789F19EA5F7301FEE48A702C2B32AF64EF69A_il2cpp_TypeInfo_var);
		Deriv__ctor_m96CA4EA0FBA14F1BA387EA264701F72CE03FCE17(L_59, __this, (intptr_t)((void*)AscentSim_Deriv_m4147891C17132CF460ABF67F65FA78578EE2235A_RuntimeMethod_var), NULL);
		__this->___deriv = L_59;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___deriv), (void*)L_59);
		List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5* L_60 = __this->___Events;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_61 = (SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9*)il2cpp_codegen_object_new(SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9_il2cpp_TypeInfo_var);
		SimEvent__ctor_m9601BFE450112DDA042CB7CB0DA82C273CCA7367(L_61, NULL);
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_62 = L_61;
		NullCheck(L_62);
		L_62->___t = (0.0);
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_63 = L_62;
		NullCheck(L_63);
		L_63->___type = _stringLiteralC22AD2B06041300DA8ABAAA301A2E02B285863B3;
		Il2CppCodeGenWriteBarrier((void**)(&L_63->___type), (void*)_stringLiteralC22AD2B06041300DA8ABAAA301A2E02B285863B3);
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_64 = L_63;
		Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28 L_65;
		memset((&L_65), 0, sizeof(L_65));
		Nullable_1__ctor_m141FA88563AC0B5179132FB929EABD02C47FF703((&L_65), 1, Nullable_1__ctor_m141FA88563AC0B5179132FB929EABD02C47FF703_RuntimeMethod_var);
		NullCheck(L_64);
		L_64->___stage = L_65;
		NullCheck(L_60);
		List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_inline(L_60, L_64, List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_RuntimeMethod_var);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void AscentSim_Deriv_m4147891C17132CF460ABF67F65FA78578EE2235A (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, double ___0_t, DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___1_s, DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___2_dy, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	double V_0 = 0.0;
	double V_1 = 0.0;
	double V_2 = 0.0;
	double V_3 = 0.0;
	double V_4 = 0.0;
	double V_5 = 0.0;
	double V_6 = 0.0;
	double V_7 = 0.0;
	double V_8 = 0.0;
	double V_9 = 0.0;
	double V_10 = 0.0;
	double V_11 = 0.0;
	double V_12 = 0.0;
	double V_13 = 0.0;
	double V_14 = 0.0;
	double V_15 = 0.0;
	double V_16 = 0.0;
	double V_17 = 0.0;
	double V_18 = 0.0;
	double V_19 = 0.0;
	double V_20 = 0.0;
	SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* V_21 = NULL;
	double V_22 = 0.0;
	SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* V_23 = NULL;
	double V_24 = 0.0;
	double V_25 = 0.0;
	double V_26 = 0.0;
	double V_27 = 0.0;
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_0 = ___1_s;
		NullCheck(L_0);
		int32_t L_1 = 0;
		double L_2 = (L_0)->GetAt(static_cast<il2cpp_array_size_t>(L_1));
		V_0 = L_2;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_3 = ___1_s;
		NullCheck(L_3);
		int32_t L_4 = 1;
		double L_5 = (L_3)->GetAt(static_cast<il2cpp_array_size_t>(L_4));
		V_1 = L_5;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_6 = ___1_s;
		NullCheck(L_6);
		int32_t L_7 = 2;
		double L_8 = (L_6)->GetAt(static_cast<il2cpp_array_size_t>(L_7));
		V_2 = L_8;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_9 = ___1_s;
		NullCheck(L_9);
		int32_t L_10 = 3;
		double L_11 = (L_9)->GetAt(static_cast<il2cpp_array_size_t>(L_10));
		V_3 = L_11;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_12 = ___1_s;
		NullCheck(L_12);
		int32_t L_13 = 4;
		double L_14 = (L_12)->GetAt(static_cast<il2cpp_array_size_t>(L_13));
		V_4 = L_14;
		double L_15 = V_0;
		double L_16 = V_0;
		double L_17 = V_1;
		double L_18 = V_1;
		V_5 = ((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_15, L_16)), ((double)il2cpp_codegen_multiply(L_17, L_18))));
		double L_19 = V_5;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_20;
		L_20 = sqrt(L_19);
		V_6 = L_20;
		double L_21 = V_5;
		double L_22 = V_6;
		double L_23 = ((double)((-398600441800000.0)/((double)il2cpp_codegen_multiply(L_21, L_22))));
		double L_24 = V_0;
		V_7 = ((double)il2cpp_codegen_multiply(L_23, L_24));
		double L_25 = V_1;
		V_8 = ((double)il2cpp_codegen_multiply(L_23, L_25));
		V_9 = (0.0);
		double L_26 = V_0;
		double L_27 = V_6;
		V_10 = ((double)(L_26/L_27));
		double L_28 = V_1;
		double L_29 = V_6;
		V_11 = ((double)(L_28/L_29));
		double L_30 = V_11;
		V_12 = ((-L_30));
		double L_31 = V_10;
		V_13 = L_31;
		double L_32 = V_6;
		V_14 = ((double)il2cpp_codegen_subtract(L_32, (6378137.0)));
		V_15 = (0.0);
		V_16 = (0.0);
		V_17 = (340.0);
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_33 = __this->___o;
		NullCheck(L_33);
		bool L_34 = L_33->___vacuum;
		if (L_34)
		{
			goto IL_00c6;
		}
	}
	{
		double L_35 = V_14;
		if ((!(((double)L_35) < ((double)(300000.0)))))
		{
			goto IL_00c6;
		}
	}
	{
		double L_36 = V_14;
		AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 L_37;
		L_37 = Atmosphere_At_mA6E038B8FA424E44AD2A97805AC95A264333ECF7(L_36, NULL);
		AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 L_38 = L_37;
		double L_39 = L_38.___p;
		V_15 = L_39;
		AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 L_40 = L_38;
		double L_41 = L_40.___rho;
		V_16 = L_41;
		double L_42 = L_40.___a;
		V_17 = L_42;
	}

IL_00c6:
	{
		double L_43 = V_2;
		double L_44 = __this->___omegaAir;
		double L_45 = V_1;
		V_18 = ((double)il2cpp_codegen_add(L_43, ((double)il2cpp_codegen_multiply(L_44, L_45))));
		double L_46 = V_3;
		double L_47 = __this->___omegaAir;
		double L_48 = V_0;
		V_19 = ((double)il2cpp_codegen_subtract(L_46, ((double)il2cpp_codegen_multiply(L_47, L_48))));
		double L_49 = V_18;
		double L_50 = V_18;
		double L_51 = V_19;
		double L_52 = V_19;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_53;
		L_53 = sqrt(((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_49, L_50)), ((double)il2cpp_codegen_multiply(L_51, L_52)))));
		V_20 = L_53;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_54 = __this->___o;
		NullCheck(L_54);
		SeededWind_t078A40A35FF4627B5FA2AA1AE748CF01648BD194* L_55 = L_54->___wind;
		if (!L_55)
		{
			goto IL_013e;
		}
	}
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_56 = __this->___o;
		NullCheck(L_56);
		bool L_57 = L_56->___vacuum;
		if (L_57)
		{
			goto IL_013e;
		}
	}
	{
		double L_58 = V_14;
		if ((!(((double)L_58) < ((double)(300000.0)))))
		{
			goto IL_013e;
		}
	}
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_59 = __this->___o;
		NullCheck(L_59);
		SeededWind_t078A40A35FF4627B5FA2AA1AE748CF01648BD194* L_60 = L_59->___wind;
		double L_61 = V_14;
		double L_62 = V_12;
		double L_63 = V_13;
		double L_64 = V_20;
		NullCheck(L_60);
		double L_65;
		L_65 = SeededWind_Retarget_m66ABCD42DE0C7ED4ED63C5E0F0F9FA063D10A1B1(L_60, L_61, L_62, L_63, (&V_18), (&V_19), L_64, (0.0), NULL);
		V_20 = L_65;
	}

IL_013e:
	{
		double L_66 = V_16;
		if ((!(((double)L_66) > ((double)(0.0)))))
		{
			goto IL_01c4;
		}
	}
	{
		double L_67 = V_20;
		if ((!(((double)L_67) > ((double)(0.0)))))
		{
			goto IL_01c4;
		}
	}
	{
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_68 = __this->___st;
		int32_t L_69;
		L_69 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_70 = __this->___st;
		NullCheck(L_70);
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		int32_t L_71;
		L_71 = Math_Min_m53C488772A34D53917BCA2A491E79A0A5356ED52(L_69, ((int32_t)il2cpp_codegen_subtract(((int32_t)(((RuntimeArray*)L_70)->max_length)), 1)), NULL);
		NullCheck(L_68);
		int32_t L_72 = L_71;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_73 = (L_68)->GetAt(static_cast<il2cpp_array_size_t>(L_72));
		V_21 = L_73;
		double L_74 = V_16;
		double L_75 = V_20;
		double L_76 = V_20;
		double L_77 = V_20;
		double L_78 = V_17;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_79 = V_21;
		NullCheck(L_79);
		double L_80 = L_79->___cd;
		double L_81;
		L_81 = Aero_CdMach_m0F78C3ADE3138A85FBDF51DC71A3040C035EED47(((double)(L_77/L_78)), L_80, NULL);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_82 = V_21;
		NullCheck(L_82);
		double L_83 = L_82->___area;
		V_22 = ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((0.5), L_74)), L_75)), L_76)), L_81)), L_83));
		double L_84 = V_7;
		double L_85 = V_22;
		double L_86 = V_18;
		double L_87 = V_20;
		double L_88 = V_4;
		V_7 = ((double)il2cpp_codegen_subtract(L_84, ((double)(((double)il2cpp_codegen_multiply(L_85, L_86))/((double)il2cpp_codegen_multiply(L_87, L_88))))));
		double L_89 = V_8;
		double L_90 = V_22;
		double L_91 = V_19;
		double L_92 = V_20;
		double L_93 = V_4;
		V_8 = ((double)il2cpp_codegen_subtract(L_89, ((double)(((double)il2cpp_codegen_multiply(L_90, L_91))/((double)il2cpp_codegen_multiply(L_92, L_93))))));
	}

IL_01c4:
	{
		bool L_94;
		L_94 = AscentSim_get_Thrusting_m1221CE066B159B7647B0988CCA625853DB547A22_inline(__this, NULL);
		if (!L_94)
		{
			goto IL_02b9;
		}
	}
	{
		int32_t L_95;
		L_95 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_96 = __this->___st;
		NullCheck(L_96);
		if ((((int32_t)L_95) >= ((int32_t)((int32_t)(((RuntimeArray*)L_96)->max_length)))))
		{
			goto IL_02b9;
		}
	}
	{
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_97 = __this->___st;
		int32_t L_98;
		L_98 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		NullCheck(L_97);
		int32_t L_99 = L_98;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_100 = (L_97)->GetAt(static_cast<il2cpp_array_size_t>(L_99));
		V_23 = L_100;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_101 = V_23;
		NullCheck(L_101);
		double L_102 = L_101->___mdot;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_103 = V_23;
		NullCheck(L_103);
		double L_104 = L_103->___ispSL;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_105 = V_23;
		NullCheck(L_105);
		double L_106 = L_105->___ispVac;
		double L_107 = V_15;
		double L_108;
		L_108 = RocketEq_IspAt_m4FFE489F4D4A8AD0D4199B175E2B729E0727C9B6(L_104, L_106, L_107, NULL);
		V_24 = ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_102, (9.8066499999999994))), L_108));
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_109 = V_23;
		NullCheck(L_109);
		double L_110 = L_109->___mdot;
		V_9 = L_110;
		double L_111 = __this->___thrustScale;
		if ((((double)L_111) == ((double)(1.0))))
		{
			goto IL_024a;
		}
	}
	{
		double L_112 = V_24;
		double L_113 = __this->___thrustScale;
		V_24 = ((double)il2cpp_codegen_multiply(L_112, L_113));
		double L_114 = V_9;
		double L_115 = __this->___thrustScale;
		V_9 = ((double)il2cpp_codegen_multiply(L_114, L_115));
	}

IL_024a:
	{
		int32_t L_116;
		L_116 = AscentSim_get_Mode_m5522E937FEF440F09E1F28A50A534066FEA2E2C9_inline(__this, NULL);
		if ((!(((uint32_t)L_116) == ((uint32_t)2))))
		{
			goto IL_0270;
		}
	}
	{
		double L_117 = V_20;
		if ((!(((double)L_117) > ((double)(1.0)))))
		{
			goto IL_0270;
		}
	}
	{
		double L_118 = V_18;
		double L_119 = V_20;
		V_25 = ((double)(L_118/L_119));
		double L_120 = V_19;
		double L_121 = V_20;
		V_26 = ((double)(L_120/L_121));
		goto IL_029f;
	}

IL_0270:
	{
		double L_122;
		L_122 = AscentSim_get_PitchCmd_mC76BA7E009D9C12D9F966E247CC7909311A135ED_inline(__this, NULL);
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_123;
		L_123 = cos(L_122);
		double L_124;
		L_124 = AscentSim_get_PitchCmd_mC76BA7E009D9C12D9F966E247CC7909311A135ED_inline(__this, NULL);
		double L_125;
		L_125 = sin(L_124);
		V_27 = L_125;
		double L_126 = L_123;
		double L_127 = V_12;
		double L_128 = V_27;
		double L_129 = V_10;
		V_25 = ((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_126, L_127)), ((double)il2cpp_codegen_multiply(L_128, L_129))));
		double L_130 = V_13;
		double L_131 = V_27;
		double L_132 = V_11;
		V_26 = ((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_126, L_130)), ((double)il2cpp_codegen_multiply(L_131, L_132))));
	}

IL_029f:
	{
		double L_133 = V_7;
		double L_134 = V_24;
		double L_135 = V_25;
		double L_136 = V_4;
		V_7 = ((double)il2cpp_codegen_add(L_133, ((double)(((double)il2cpp_codegen_multiply(L_134, L_135))/L_136))));
		double L_137 = V_8;
		double L_138 = V_24;
		double L_139 = V_26;
		double L_140 = V_4;
		V_8 = ((double)il2cpp_codegen_add(L_137, ((double)(((double)il2cpp_codegen_multiply(L_138, L_139))/L_140))));
	}

IL_02b9:
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_141 = ___2_dy;
		double L_142 = V_2;
		NullCheck(L_141);
		(L_141)->SetAt(static_cast<il2cpp_array_size_t>(0), (double)L_142);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_143 = ___2_dy;
		double L_144 = V_3;
		NullCheck(L_143);
		(L_143)->SetAt(static_cast<il2cpp_array_size_t>(1), (double)L_144);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_145 = ___2_dy;
		double L_146 = V_7;
		NullCheck(L_145);
		(L_145)->SetAt(static_cast<il2cpp_array_size_t>(2), (double)L_146);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_147 = ___2_dy;
		double L_148 = V_8;
		NullCheck(L_147);
		(L_147)->SetAt(static_cast<il2cpp_array_size_t>(3), (double)L_148);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_149 = ___2_dy;
		double L_150 = V_9;
		NullCheck(L_149);
		(L_149)->SetAt(static_cast<il2cpp_array_size_t>(4), (double)((-L_150)));
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double AscentSim_StageEndMass_mB2D815CE13FEE7A63EAA69082D706CB616D6C623 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_0 = __this->___upperMass;
		int32_t L_1;
		L_1 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		NullCheck(L_0);
		int32_t L_2 = L_1;
		double L_3 = (L_0)->GetAt(static_cast<il2cpp_array_size_t>(L_2));
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_4 = __this->___st;
		int32_t L_5;
		L_5 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		NullCheck(L_4);
		int32_t L_6 = L_5;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_7 = (L_4)->GetAt(static_cast<il2cpp_array_size_t>(L_6));
		NullCheck(L_7);
		double L_8 = L_7->___propMass;
		return ((double)il2cpp_codegen_subtract(L_3, L_8));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool AscentSim_Tick_m3AE54AD6763AE540AEC122CE9F9AF31974596B28 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IFlightSimSink_t6B2E44FEAA2B83A736D6ADA46993E42135AF5A95_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_Add_m67EF1200FCD49355EECE77EB754C2EED52F8B3DE_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Nullable_1__ctor_m141FA88563AC0B5179132FB929EABD02C47FF703_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral1E3DB61522C4E7AA1D5A216FFA2CA92BAFA2EBF1);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral48DF3B432B8D62D5F8D80DE53D4D9FCC446CD284);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral88CE3FCD887A58F8A5764032FA39B0C1CD48440B);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralA2B26C8EC14B2C05545E961B44B92631AA65EBE5);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralAD88ECBD135D7E7861EE52F538BEC1F9767085EA);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralADFC4849E68D1978E71A9D4530D530AB1F600C39);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralC22AD2B06041300DA8ABAAA301A2E02B285863B3);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralCDE83734553D6AC0E1530734343508D1FC27A13F);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralD991FE6C9AC0B4EDFA368D323F13614F48EBFCB3);
		s_Il2CppMethodInitialized = true;
	}
	double V_0 = 0.0;
	double V_1 = 0.0;
	double V_2 = 0.0;
	double V_3 = 0.0;
	double V_4 = 0.0;
	double V_5 = 0.0;
	double V_6 = 0.0;
	double V_7 = 0.0;
	double V_8 = 0.0;
	double V_9 = 0.0;
	double V_10 = 0.0;
	double V_11 = 0.0;
	double V_12 = 0.0;
	double V_13 = 0.0;
	double V_14 = 0.0;
	double V_15 = 0.0;
	double V_16 = 0.0;
	double V_17 = 0.0;
	double V_18 = 0.0;
	double V_19 = 0.0;
	double V_20 = 0.0;
	double V_21 = 0.0;
	double V_22 = 0.0;
	double V_23 = 0.0;
	double V_24 = 0.0;
	bool V_25 = false;
	double V_26 = 0.0;
	double V_27 = 0.0;
	double V_28 = 0.0;
	SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* V_29 = NULL;
	AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 V_30;
	memset((&V_30), 0, sizeof(V_30));
	double V_31 = 0.0;
	double V_32 = 0.0;
	double V_33 = 0.0;
	double V_34 = 0.0;
	ValueTuple_2_t6535F6090E6D3C9A7A645F4BFD8ED400CBA8B14C V_35;
	memset((&V_35), 0, sizeof(V_35));
	double V_36 = 0.0;
	double V_37 = 0.0;
	double V_38 = 0.0;
	double V_39 = 0.0;
	double V_40 = 0.0;
	double V_41 = 0.0;
	bool V_42 = false;
	bool V_43 = false;
	int32_t V_44 = 0;
	SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* V_45 = NULL;
	double V_46 = 0.0;
	double V_47 = 0.0;
	double V_48 = 0.0;
	double V_49 = 0.0;
	double V_50 = 0.0;
	double V_51 = 0.0;
	double V_52 = 0.0;
	double V_53 = 0.0;
	SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121 V_54;
	memset((&V_54), 0, sizeof(V_54));
	double V_55 = 0.0;
	double V_56 = 0.0;
	double V_57 = 0.0;
	OrbitElements_t5EEC91C4BE0D34654FC26DBE704E101E14BB7D98 V_58;
	memset((&V_58), 0, sizeof(V_58));
	AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* G_B11_0 = NULL;
	AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* G_B10_0 = NULL;
	double G_B12_0 = 0.0;
	AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* G_B12_1 = NULL;
	double G_B30_0 = 0.0;
	double G_B30_1 = 0.0;
	double G_B30_2 = 0.0;
	double G_B29_0 = 0.0;
	double G_B29_1 = 0.0;
	double G_B29_2 = 0.0;
	double G_B31_0 = 0.0;
	double G_B31_1 = 0.0;
	double G_B31_2 = 0.0;
	double G_B31_3 = 0.0;
	double G_B78_0 = 0.0;
	double G_B77_0 = 0.0;
	double G_B79_0 = 0.0;
	double G_B79_1 = 0.0;
	RuntimeObject* G_B90_0 = NULL;
	RuntimeObject* G_B89_0 = NULL;
	double G_B105_0 = 0.0;
	double G_B108_0 = 0.0;
	RuntimeObject* G_B117_0 = NULL;
	RuntimeObject* G_B116_0 = NULL;
	RuntimeObject* G_B127_0 = NULL;
	RuntimeObject* G_B126_0 = NULL;
	double G_B135_0 = 0.0;
	double G_B144_0 = 0.0;
	RuntimeObject* G_B146_0 = NULL;
	RuntimeObject* G_B145_0 = NULL;
	{
		bool L_0;
		L_0 = AscentSim_get_Done_mBC8EEA714EDD0073C9F4D91135D515D022B41A6A_inline(__this, NULL);
		if (!L_0)
		{
			goto IL_000a;
		}
	}
	{
		return (bool)0;
	}

IL_000a:
	{
		AscentSim_EnsureIgnition_mD7BE0F38DF384B744ACA8D0BF38AE349D6149F01(__this, NULL);
		double L_1;
		L_1 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_2 = __this->___o;
		NullCheck(L_2);
		double L_3 = L_2->___tMax;
		if ((((double)L_1) < ((double)L_3)))
		{
			goto IL_002c;
		}
	}
	{
		AscentSim_set_Done_m95D7993A7D005264297DDDDF34649C09515E702C_inline(__this, (bool)1, NULL);
		return (bool)0;
	}

IL_002c:
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_4 = __this->___Y;
		NullCheck(L_4);
		int32_t L_5 = 0;
		double L_6 = (L_4)->GetAt(static_cast<il2cpp_array_size_t>(L_5));
		V_0 = L_6;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_7 = __this->___Y;
		NullCheck(L_7);
		int32_t L_8 = 1;
		double L_9 = (L_7)->GetAt(static_cast<il2cpp_array_size_t>(L_8));
		V_1 = L_9;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_10 = __this->___Y;
		NullCheck(L_10);
		int32_t L_11 = 2;
		double L_12 = (L_10)->GetAt(static_cast<il2cpp_array_size_t>(L_11));
		V_2 = L_12;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_13 = __this->___Y;
		NullCheck(L_13);
		int32_t L_14 = 3;
		double L_15 = (L_13)->GetAt(static_cast<il2cpp_array_size_t>(L_14));
		V_3 = L_15;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_16 = __this->___Y;
		NullCheck(L_16);
		int32_t L_17 = 4;
		double L_18 = (L_16)->GetAt(static_cast<il2cpp_array_size_t>(L_17));
		V_4 = L_18;
		double L_19 = V_0;
		double L_20 = V_1;
		double L_21;
		L_21 = JsMath_Hypot_mACE54445FD52A26990FD1AD7C43FDC73B6F893D4(L_19, L_20, NULL);
		V_5 = L_21;
		double L_22 = V_5;
		V_6 = ((double)il2cpp_codegen_subtract(L_22, (6378137.0)));
		double L_23 = V_0;
		double L_24 = V_5;
		V_7 = ((double)(L_23/L_24));
		double L_25 = V_1;
		double L_26 = V_5;
		V_8 = ((double)(L_25/L_26));
		double L_27 = V_8;
		V_9 = ((-L_27));
		double L_28 = V_7;
		V_10 = L_28;
		double L_29 = V_2;
		double L_30 = V_7;
		double L_31 = V_3;
		double L_32 = V_8;
		V_11 = ((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_29, L_30)), ((double)il2cpp_codegen_multiply(L_31, L_32))));
		double L_33 = V_2;
		double L_34 = V_9;
		double L_35 = V_3;
		double L_36 = V_10;
		V_12 = ((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_33, L_34)), ((double)il2cpp_codegen_multiply(L_35, L_36))));
		double L_37 = V_2;
		double L_38 = __this->___omegaAir;
		double L_39 = V_1;
		V_13 = ((double)il2cpp_codegen_add(L_37, ((double)il2cpp_codegen_multiply(L_38, L_39))));
		double L_40 = V_3;
		double L_41 = __this->___omegaAir;
		double L_42 = V_0;
		V_14 = ((double)il2cpp_codegen_subtract(L_40, ((double)il2cpp_codegen_multiply(L_41, L_42))));
		double L_43 = V_13;
		double L_44 = V_14;
		double L_45;
		L_45 = JsMath_Hypot_mACE54445FD52A26990FD1AD7C43FDC73B6F893D4(L_43, L_44, NULL);
		V_15 = L_45;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_46 = __this->___o;
		NullCheck(L_46);
		SeededWind_t078A40A35FF4627B5FA2AA1AE748CF01648BD194* L_47 = L_46->___wind;
		if (!L_47)
		{
			goto IL_010d;
		}
	}
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_48 = __this->___o;
		NullCheck(L_48);
		bool L_49 = L_48->___vacuum;
		if (L_49)
		{
			goto IL_010d;
		}
	}
	{
		double L_50 = V_6;
		if ((!(((double)L_50) < ((double)(300000.0)))))
		{
			goto IL_010d;
		}
	}
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_51 = __this->___o;
		NullCheck(L_51);
		SeededWind_t078A40A35FF4627B5FA2AA1AE748CF01648BD194* L_52 = L_51->___wind;
		double L_53 = V_6;
		double L_54 = V_9;
		double L_55 = V_10;
		double L_56 = V_15;
		NullCheck(L_52);
		double L_57;
		L_57 = SeededWind_Retarget_m66ABCD42DE0C7ED4ED63C5E0F0F9FA063D10A1B1(L_52, L_53, L_54, L_55, (&V_13), (&V_14), L_56, (0.0), NULL);
		V_15 = L_57;
	}

IL_010d:
	{
		bool L_58;
		L_58 = AscentSim_get_Thrusting_m1221CE066B159B7647B0988CCA625853DB547A22_inline(__this, NULL);
		if (!L_58)
		{
			goto IL_05d5;
		}
	}
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_59 = __this->___o;
		NullCheck(L_59);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_60 = L_59->___monitor;
		if (L_60)
		{
			G_B11_0 = __this;
			goto IL_0131;
		}
		G_B10_0 = __this;
	}
	{
		G_B12_0 = (1.0);
		G_B12_1 = G_B10_0;
		goto IL_0141;
	}

IL_0131:
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_61 = __this->___o;
		NullCheck(L_61);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_62 = L_61->___monitor;
		NullCheck(L_62);
		double L_63;
		L_63 = FailureMonitor_get_ThrustFactor_mCE680846A429E677E27564A6589CC614B226C99A(L_62, NULL);
		G_B12_0 = L_63;
		G_B12_1 = G_B11_0;
	}

IL_0141:
	{
		NullCheck(G_B12_1);
		G_B12_1->___thrustScale = G_B12_0;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_64 = __this->___o;
		NullCheck(L_64);
		GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307* L_65 = L_64->___guidance;
		bool L_66;
		L_66 = Headless_LegacyPitch_m7901412AEEAE5BE07A9992D2FD9FF0D30EADE09C(L_65, NULL);
		if (!L_66)
		{
			goto IL_0461;
		}
	}
	{
		double L_67 = V_6;
		double L_68 = V_4;
		double L_69 = V_11;
		double L_70 = V_15;
		bool L_71;
		L_71 = AscentSim_HoldingMaxQPitch_m6A1276F1456AD0B8E5EA145533B867A7449BD87B(__this, L_67, L_68, L_69, L_70, NULL);
		V_25 = L_71;
		int32_t L_72;
		L_72 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		if (L_72)
		{
			goto IL_02ba;
		}
	}
	{
		int32_t L_73;
		L_73 = AscentSim_get_Mode_m5522E937FEF440F09E1F28A50A534066FEA2E2C9_inline(__this, NULL);
		if (L_73)
		{
			goto IL_01bb;
		}
	}
	{
		double L_74 = V_15;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_75 = __this->___o;
		NullCheck(L_75);
		double L_76 = L_75->___kickSpeed;
		if ((!(((double)L_74) >= ((double)L_76))))
		{
			goto IL_01bb;
		}
	}
	{
		AscentSim_set_Mode_m7946FC27FD607C87B37DB52807785705728CFDC3_inline(__this, 1, NULL);
		double L_77;
		L_77 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		__this->___kickStart = L_77;
		double L_78 = __this->___kickAngle;
		AscentSim_set_PitchCmd_m54C6E65BF6705E8F4DD4DC357074BD0DF8CA677A_inline(__this, ((double)il2cpp_codegen_subtract((1.5707963267948966), L_78)), NULL);
		goto IL_03c0;
	}

IL_01bb:
	{
		int32_t L_79;
		L_79 = AscentSim_get_Mode_m5522E937FEF440F09E1F28A50A534066FEA2E2C9_inline(__this, NULL);
		if ((!(((uint32_t)L_79) == ((uint32_t)1))))
		{
			goto IL_0261;
		}
	}
	{
		double L_80 = V_13;
		double L_81 = V_7;
		double L_82 = V_14;
		double L_83 = V_8;
		double L_84 = V_13;
		double L_85 = V_9;
		double L_86 = V_14;
		double L_87 = V_10;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_88;
		L_88 = atan2(((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_80, L_81)), ((double)il2cpp_codegen_multiply(L_82, L_83)))), ((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_84, L_85)), ((double)il2cpp_codegen_multiply(L_86, L_87)))));
		V_26 = L_88;
		bool L_89 = __this->___turned;
		bool L_90 = V_25;
		if (!((int32_t)((int32_t)L_89&(int32_t)L_90)))
		{
			goto IL_0200;
		}
	}
	{
		double L_91 = __this->___catchPitch;
		AscentSim_set_PitchCmd_m54C6E65BF6705E8F4DD4DC357074BD0DF8CA677A_inline(__this, L_91, NULL);
		goto IL_03c0;
	}

IL_0200:
	{
		bool L_92 = __this->___turned;
		if (!L_92)
		{
			goto IL_0214;
		}
	}
	{
		AscentSim_set_Mode_m7946FC27FD607C87B37DB52807785705728CFDC3_inline(__this, 2, NULL);
		goto IL_03c0;
	}

IL_0214:
	{
		double L_93;
		L_93 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		double L_94 = __this->___kickStart;
		if ((!(((double)((double)il2cpp_codegen_subtract(L_93, L_94))) > ((double)(3.0)))))
		{
			goto IL_03c0;
		}
	}
	{
		double L_95 = V_26;
		double L_96;
		L_96 = AscentSim_get_PitchCmd_mC76BA7E009D9C12D9F966E247CC7909311A135ED_inline(__this, NULL);
		if ((!(((double)L_95) <= ((double)((double)il2cpp_codegen_add(L_96, (0.002)))))))
		{
			goto IL_03c0;
		}
	}
	{
		double L_97 = V_26;
		__this->___catchPitch = L_97;
		__this->___turned = (bool)1;
		AscentSim_set_Mode_m7946FC27FD607C87B37DB52807785705728CFDC3_inline(__this, 2, NULL);
		goto IL_03c0;
	}

IL_0261:
	{
		int32_t L_98;
		L_98 = AscentSim_get_Mode_m5522E937FEF440F09E1F28A50A534066FEA2E2C9_inline(__this, NULL);
		bool L_99 = V_25;
		if (!((int32_t)(((((int32_t)L_98) == ((int32_t)2))? 1 : 0)&(int32_t)L_99)))
		{
			goto IL_03c0;
		}
	}
	{
		double L_100 = V_13;
		double L_101 = V_7;
		double L_102 = V_14;
		double L_103 = V_8;
		double L_104 = V_13;
		double L_105 = V_9;
		double L_106 = V_14;
		double L_107 = V_10;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_108;
		L_108 = atan2(((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_100, L_101)), ((double)il2cpp_codegen_multiply(L_102, L_103)))), ((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_104, L_105)), ((double)il2cpp_codegen_multiply(L_106, L_107)))));
		double L_109 = __this->___catchPitch;
		if ((!(((double)L_108) < ((double)((double)il2cpp_codegen_subtract(L_109, (0.002)))))))
		{
			goto IL_03c0;
		}
	}
	{
		double L_110 = __this->___catchPitch;
		AscentSim_set_PitchCmd_m54C6E65BF6705E8F4DD4DC357074BD0DF8CA677A_inline(__this, L_110, NULL);
		AscentSim_set_Mode_m7946FC27FD607C87B37DB52807785705728CFDC3_inline(__this, 1, NULL);
		goto IL_03c0;
	}

IL_02ba:
	{
		AscentSim_set_Mode_m7946FC27FD607C87B37DB52807785705728CFDC3_inline(__this, 3, NULL);
		double L_111 = V_5;
		double L_112 = V_5;
		double L_113 = V_12;
		double L_114 = V_12;
		double L_115 = V_5;
		V_27 = ((double)il2cpp_codegen_subtract(((double)((398600441800000.0)/((double)il2cpp_codegen_multiply(L_111, L_112)))), ((double)(((double)il2cpp_codegen_multiply(L_113, L_114))/L_115))));
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_116 = __this->___o;
		NullCheck(L_116);
		double L_117 = L_116->___targetAlt;
		double L_118 = V_6;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_119;
		L_119 = Math_Min_mA3310F1FF7876DA2FC7F37B822E6DD66410565C1((250.0), ((double)(((double)il2cpp_codegen_subtract(L_117, L_118))/(40.0))), NULL);
		double L_120;
		L_120 = Math_Max_m7BAC743E1752A51F258BB82DEBDD13E7C6D3ED26((-150.0), L_119, NULL);
		V_28 = L_120;
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_121 = __this->___st;
		int32_t L_122;
		L_122 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		NullCheck(L_121);
		int32_t L_123 = L_122;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_124 = (L_121)->GetAt(static_cast<il2cpp_array_size_t>(L_123));
		V_29 = L_124;
		double L_125 = V_6;
		double L_126;
		L_126 = Math_Min_mA3310F1FF7876DA2FC7F37B822E6DD66410565C1(L_125, (300000.0), NULL);
		AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 L_127;
		L_127 = Atmosphere_At_mA6E038B8FA424E44AD2A97805AC95A264333ECF7(L_126, NULL);
		V_30 = L_127;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_128 = V_29;
		NullCheck(L_128);
		double L_129 = L_128->___mdot;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_130 = V_29;
		NullCheck(L_130);
		double L_131 = L_130->___ispSL;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_132 = V_29;
		NullCheck(L_132);
		double L_133 = L_132->___ispVac;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_134 = __this->___o;
		NullCheck(L_134);
		bool L_135 = L_134->___vacuum;
		if (L_135)
		{
			G_B30_0 = L_133;
			G_B30_1 = L_131;
			G_B30_2 = ((double)il2cpp_codegen_multiply(L_129, (9.8066499999999994)));
			goto IL_036c;
		}
		G_B29_0 = L_133;
		G_B29_1 = L_131;
		G_B29_2 = ((double)il2cpp_codegen_multiply(L_129, (9.8066499999999994)));
	}
	{
		AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 L_136 = V_30;
		double L_137 = L_136.___p;
		G_B31_0 = L_137;
		G_B31_1 = G_B29_0;
		G_B31_2 = G_B29_1;
		G_B31_3 = G_B29_2;
		goto IL_0375;
	}

IL_036c:
	{
		G_B31_0 = (0.0);
		G_B31_1 = G_B30_0;
		G_B31_2 = G_B30_1;
		G_B31_3 = G_B30_2;
	}

IL_0375:
	{
		double L_138;
		L_138 = RocketEq_IspAt_m4FFE489F4D4A8AD0D4199B175E2B729E0727C9B6(G_B31_2, G_B31_1, G_B31_0, NULL);
		double L_139 = V_4;
		V_31 = ((double)(((double)il2cpp_codegen_multiply(G_B31_3, L_138))/L_139));
		double L_140 = V_27;
		double L_141 = V_28;
		double L_142 = V_11;
		double L_143 = V_31;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_144;
		L_144 = Math_Min_mA3310F1FF7876DA2FC7F37B822E6DD66410565C1((0.94999999999999996), ((double)(((double)il2cpp_codegen_add(L_140, ((double)il2cpp_codegen_multiply((0.029999999999999999), ((double)il2cpp_codegen_subtract(L_141, L_142))))))/L_143)), NULL);
		double L_145;
		L_145 = Math_Max_m7BAC743E1752A51F258BB82DEBDD13E7C6D3ED26((-0.59999999999999998), L_144, NULL);
		V_32 = L_145;
		double L_146 = V_32;
		double L_147;
		L_147 = asin(L_146);
		AscentSim_set_PitchCmd_m54C6E65BF6705E8F4DD4DC357074BD0DF8CA677A_inline(__this, L_147, NULL);
	}

IL_03c0:
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_148 = __this->___o;
		NullCheck(L_148);
		GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307* L_149 = L_148->___guidance;
		if (!L_149)
		{
			goto IL_050d;
		}
	}
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_150 = __this->___o;
		NullCheck(L_150);
		GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307* L_151 = L_150->___guidance;
		NullCheck(L_151);
		bool L_152 = L_151->___maxQThrottleSet;
		if (L_152)
		{
			goto IL_03f7;
		}
	}
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_153 = __this->___o;
		NullCheck(L_153);
		GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307* L_154 = L_153->___guidance;
		NullCheck(L_154);
		bool L_155 = L_154->___gLimitSet;
		if (!L_155)
		{
			goto IL_050d;
		}
	}

IL_03f7:
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_156 = __this->___o;
		NullCheck(L_156);
		GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307* L_157 = L_156->___guidance;
		double L_158 = V_6;
		double L_159 = V_4;
		double L_160 = V_11;
		double L_161 = V_5;
		double L_162 = V_5;
		double L_163 = V_12;
		double L_164 = V_12;
		double L_165 = V_5;
		double L_166 = V_15;
		GuidanceState_tA40A6AE2010623D64FC40402D0D31113286A0401 L_167;
		L_167 = AscentSim_Guide_mD6EC3412C6E84F3BB7683B700B76EE6FBA7235E1(__this, L_158, L_159, L_160, ((double)il2cpp_codegen_subtract(((double)((398600441800000.0)/((double)il2cpp_codegen_multiply(L_161, L_162)))), ((double)(((double)il2cpp_codegen_multiply(L_163, L_164))/L_165)))), L_166, (0.0), NULL);
		NullCheck(L_157);
		ValueTuple_2_t6535F6090E6D3C9A7A645F4BFD8ED400CBA8B14C L_168;
		L_168 = GuidanceProfile_Command_m0569E0B457499A166FC5720399B70D793882CD00(L_157, L_167, NULL);
		double L_169 = L_168.___Item2;
		V_33 = L_169;
		double L_170 = V_33;
		if ((((double)L_170) == ((double)(1.0))))
		{
			goto IL_050d;
		}
	}
	{
		double L_171 = __this->___thrustScale;
		double L_172 = V_33;
		__this->___thrustScale = ((double)il2cpp_codegen_multiply(L_171, L_172));
		goto IL_050d;
	}

IL_0461:
	{
		double L_173 = V_13;
		double L_174 = V_7;
		double L_175 = V_14;
		double L_176 = V_8;
		double L_177 = V_13;
		double L_178 = V_9;
		double L_179 = V_14;
		double L_180 = V_10;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_181;
		L_181 = atan2(((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_173, L_174)), ((double)il2cpp_codegen_multiply(L_175, L_176)))), ((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_177, L_178)), ((double)il2cpp_codegen_multiply(L_179, L_180)))));
		V_34 = L_181;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_182 = __this->___o;
		NullCheck(L_182);
		GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307* L_183 = L_182->___guidance;
		double L_184 = V_6;
		double L_185 = V_4;
		double L_186 = V_11;
		double L_187 = V_5;
		double L_188 = V_5;
		double L_189 = V_12;
		double L_190 = V_12;
		double L_191 = V_5;
		double L_192 = V_15;
		double L_193 = V_34;
		GuidanceState_tA40A6AE2010623D64FC40402D0D31113286A0401 L_194;
		L_194 = AscentSim_Guide_mD6EC3412C6E84F3BB7683B700B76EE6FBA7235E1(__this, L_184, L_185, L_186, ((double)il2cpp_codegen_subtract(((double)((398600441800000.0)/((double)il2cpp_codegen_multiply(L_187, L_188)))), ((double)(((double)il2cpp_codegen_multiply(L_189, L_190))/L_191)))), L_192, L_193, NULL);
		NullCheck(L_183);
		ValueTuple_2_t6535F6090E6D3C9A7A645F4BFD8ED400CBA8B14C L_195;
		L_195 = GuidanceProfile_Command_m0569E0B457499A166FC5720399B70D793882CD00(L_183, L_194, NULL);
		V_35 = L_195;
		ValueTuple_2_t6535F6090E6D3C9A7A645F4BFD8ED400CBA8B14C L_196 = V_35;
		double L_197 = L_196.___Item1;
		AscentSim_set_PitchCmd_m54C6E65BF6705E8F4DD4DC357074BD0DF8CA677A_inline(__this, L_197, NULL);
		int32_t L_198;
		L_198 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		if ((((int32_t)L_198) <= ((int32_t)0)))
		{
			goto IL_04d7;
		}
	}
	{
		AscentSim_set_Mode_m7946FC27FD607C87B37DB52807785705728CFDC3_inline(__this, 3, NULL);
		goto IL_04e7;
	}

IL_04d7:
	{
		int32_t L_199;
		L_199 = AscentSim_get_Mode_m5522E937FEF440F09E1F28A50A534066FEA2E2C9_inline(__this, NULL);
		if ((!(((uint32_t)L_199) == ((uint32_t)2))))
		{
			goto IL_04e7;
		}
	}
	{
		AscentSim_set_Mode_m7946FC27FD607C87B37DB52807785705728CFDC3_inline(__this, 1, NULL);
	}

IL_04e7:
	{
		ValueTuple_2_t6535F6090E6D3C9A7A645F4BFD8ED400CBA8B14C L_200 = V_35;
		double L_201 = L_200.___Item2;
		if ((((double)L_201) == ((double)(1.0))))
		{
			goto IL_050d;
		}
	}
	{
		double L_202 = __this->___thrustScale;
		ValueTuple_2_t6535F6090E6D3C9A7A645F4BFD8ED400CBA8B14C L_203 = V_35;
		double L_204 = L_203.___Item2;
		__this->___thrustScale = ((double)il2cpp_codegen_multiply(L_202, L_204));
	}

IL_050d:
	{
		double L_205 = __this->___LiveThrottleScale;
		if ((((double)L_205) == ((double)(1.0))))
		{
			goto IL_057d;
		}
	}
	{
		double L_206 = __this->___LiveThrottleScale;
		V_36 = L_206;
		double L_207 = V_36;
		bool L_208;
		L_208 = Double_IsNaN_mF2BC6D1FD4813179B2CAE58D29770E42830D0883_inline(L_207, NULL);
		if (L_208)
		{
			goto IL_053c;
		}
	}
	{
		double L_209 = V_36;
		if ((!(((double)L_209) <= ((double)(0.0)))))
		{
			goto IL_0549;
		}
	}

IL_053c:
	{
		V_36 = (1.0);
		goto IL_0561;
	}

IL_0549:
	{
		double L_210 = V_36;
		if ((!(((double)L_210) > ((double)(1.0)))))
		{
			goto IL_0561;
		}
	}
	{
		V_36 = (1.0);
	}

IL_0561:
	{
		double L_211 = V_36;
		if ((((double)L_211) == ((double)(1.0))))
		{
			goto IL_057d;
		}
	}
	{
		double L_212 = __this->___thrustScale;
		double L_213 = V_36;
		__this->___thrustScale = ((double)il2cpp_codegen_multiply(L_212, L_213));
	}

IL_057d:
	{
		double L_214 = __this->___LivePitchOffsetRad;
		if ((((double)L_214) == ((double)(0.0))))
		{
			goto IL_05d5;
		}
	}
	{
		double L_215 = __this->___LivePitchOffsetRad;
		bool L_216;
		L_216 = Double_IsNaN_mF2BC6D1FD4813179B2CAE58D29770E42830D0883_inline(L_215, NULL);
		if (L_216)
		{
			goto IL_05d5;
		}
	}
	{
		double L_217 = __this->___LivePitchOffsetRad;
		V_37 = L_217;
		V_38 = (0.17453292519943295);
		double L_218 = V_37;
		double L_219 = V_38;
		if ((!(((double)L_218) > ((double)L_219))))
		{
			goto IL_05ba;
		}
	}
	{
		double L_220 = V_38;
		V_37 = L_220;
		goto IL_05c6;
	}

IL_05ba:
	{
		double L_221 = V_37;
		double L_222 = V_38;
		if ((!(((double)L_221) < ((double)((-L_222))))))
		{
			goto IL_05c6;
		}
	}
	{
		double L_223 = V_38;
		V_37 = ((-L_223));
	}

IL_05c6:
	{
		double L_224;
		L_224 = AscentSim_get_PitchCmd_mC76BA7E009D9C12D9F966E247CC7909311A135ED_inline(__this, NULL);
		double L_225 = V_37;
		AscentSim_set_PitchCmd_m54C6E65BF6705E8F4DD4DC357074BD0DF8CA677A_inline(__this, ((double)il2cpp_codegen_add(L_224, L_225)), NULL);
	}

IL_05d5:
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_226 = __this->___o;
		NullCheck(L_226);
		double L_227 = L_226->___dt;
		V_16 = L_227;
		bool L_228;
		L_228 = AscentSim_get_Thrusting_m1221CE066B159B7647B0988CCA625853DB547A22_inline(__this, NULL);
		if (!L_228)
		{
			goto IL_08a2;
		}
	}
	{
		int32_t L_229;
		L_229 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_230 = __this->___st;
		NullCheck(L_230);
		if ((((int32_t)L_229) >= ((int32_t)((int32_t)(((RuntimeArray*)L_230)->max_length)))))
		{
			goto IL_08a2;
		}
	}
	{
		double L_231 = V_4;
		double L_232;
		L_232 = AscentSim_StageEndMass_mB2D815CE13FEE7A63EAA69082D706CB616D6C623(__this, NULL);
		V_39 = ((double)il2cpp_codegen_subtract(L_231, L_232));
		double L_233 = V_39;
		if ((!(((double)L_233) <= ((double)(9.9999999999999995E-07)))))
		{
			goto IL_0841;
		}
	}
	{
		double L_234 = V_2;
		double L_235 = V_3;
		double L_236;
		L_236 = JsMath_Hypot_mACE54445FD52A26990FD1AD7C43FDC73B6F893D4(L_234, L_235, NULL);
		V_41 = L_236;
		List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5* L_237 = __this->___Events;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_238 = (SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9*)il2cpp_codegen_object_new(SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9_il2cpp_TypeInfo_var);
		SimEvent__ctor_m9601BFE450112DDA042CB7CB0DA82C273CCA7367(L_238, NULL);
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_239 = L_238;
		double L_240;
		L_240 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		NullCheck(L_239);
		L_239->___t = L_240;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_241 = L_239;
		NullCheck(L_241);
		L_241->___type = _stringLiteralA2B26C8EC14B2C05545E961B44B92631AA65EBE5;
		Il2CppCodeGenWriteBarrier((void**)(&L_241->___type), (void*)_stringLiteralA2B26C8EC14B2C05545E961B44B92631AA65EBE5);
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_242 = L_241;
		int32_t L_243;
		L_243 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28 L_244;
		memset((&L_244), 0, sizeof(L_244));
		Nullable_1__ctor_m141FA88563AC0B5179132FB929EABD02C47FF703((&L_244), ((int32_t)il2cpp_codegen_add(L_243, 1)), Nullable_1__ctor_m141FA88563AC0B5179132FB929EABD02C47FF703_RuntimeMethod_var);
		NullCheck(L_242);
		L_242->___stage = L_244;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_245 = L_242;
		double L_246 = V_6;
		Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 L_247;
		memset((&L_247), 0, sizeof(L_247));
		Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF((&L_247), L_246, Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF_RuntimeMethod_var);
		NullCheck(L_245);
		L_245->___alt = L_247;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_248 = L_245;
		double L_249 = V_41;
		Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 L_250;
		memset((&L_250), 0, sizeof(L_250));
		Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF((&L_250), L_249, Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF_RuntimeMethod_var);
		NullCheck(L_248);
		L_248->___v = L_250;
		NullCheck(L_237);
		List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_inline(L_237, L_248, List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_RuntimeMethod_var);
		V_42 = (bool)0;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_251 = __this->___o;
		NullCheck(L_251);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_252 = L_251->___monitor;
		if (!L_252)
		{
			goto IL_071e;
		}
	}
	{
		int32_t L_253;
		L_253 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_254 = __this->___st;
		NullCheck(L_254);
		V_43 = (bool)((((int32_t)((((int32_t)L_253) < ((int32_t)((int32_t)il2cpp_codegen_subtract(((int32_t)(((RuntimeArray*)L_254)->max_length)), 1))))? 1 : 0)) == ((int32_t)0))? 1 : 0);
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_255 = __this->___o;
		NullCheck(L_255);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_256 = L_255->___monitor;
		double L_257;
		L_257 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		double L_258 = V_6;
		bool L_259 = V_43;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_260 = __this->___o;
		NullCheck(L_260);
		double L_261 = L_260->___targetAlt;
		NullCheck(L_256);
		bool L_262;
		L_262 = FailureMonitor_OnEmpty_m31D4D1AE7462FA83EC9CA94838F8F2A3E1F7CF0E(L_256, L_257, L_258, L_259, L_261, NULL);
		if (!L_262)
		{
			goto IL_06cc;
		}
	}
	{
		V_42 = (bool)1;
	}

IL_06cc:
	{
		bool L_263 = V_43;
		if (L_263)
		{
			goto IL_071e;
		}
	}
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_264 = __this->___o;
		NullCheck(L_264);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_265 = L_264->___monitor;
		double L_266;
		L_266 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		double L_267 = V_6;
		double L_268 = V_6;
		double L_269 = V_15;
		double L_270;
		L_270 = AscentSim_Dynamic_m84A8DC43B7B0F22A9CBD7991BB4F2D56A8A6693B(__this, L_268, L_269, NULL);
		int32_t L_271;
		L_271 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_272 = __this->___st;
		int32_t L_273;
		L_273 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		NullCheck(L_272);
		int32_t L_274 = L_273;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_275 = (L_272)->GetAt(static_cast<il2cpp_array_size_t>(L_274));
		NullCheck(L_275);
		double L_276 = L_275->___dryMass;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_277 = __this->___upperMass;
		int32_t L_278;
		L_278 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		NullCheck(L_277);
		int32_t L_279 = ((int32_t)il2cpp_codegen_add(L_278, 1));
		double L_280 = (L_277)->GetAt(static_cast<il2cpp_array_size_t>(L_279));
		NullCheck(L_265);
		bool L_281;
		L_281 = FailureMonitor_OnSeparation_mF74CBB8B5B72CB981F36FED8ED6EB115880E33AD(L_265, L_266, L_267, L_270, L_271, L_276, L_280, NULL);
		if (!L_281)
		{
			goto IL_071e;
		}
	}
	{
		V_42 = (bool)1;
	}

IL_071e:
	{
		bool L_282 = V_42;
		if (!L_282)
		{
			goto IL_072e;
		}
	}
	{
		double L_283 = V_6;
		double L_284 = V_41;
		AscentSim_Fail_mF30CBB115E75C16458AE75592AA73960EAF8B1F9(__this, L_283, L_284, NULL);
		return (bool)1;
	}

IL_072e:
	{
		int32_t L_285;
		L_285 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		V_44 = L_285;
		int32_t L_286 = V_44;
		AscentSim_set_StageIdx_m0D20B990A04C0A885ABBD3A3B4861165D5091CAF_inline(__this, ((int32_t)il2cpp_codegen_add(L_286, 1)), NULL);
		int32_t L_287;
		L_287 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_288 = __this->___st;
		NullCheck(L_288);
		if ((((int32_t)L_287) < ((int32_t)((int32_t)(((RuntimeArray*)L_288)->max_length)))))
		{
			goto IL_0760;
		}
	}
	{
		AscentSim_set_Thrusting_mD9D054E78DF826C6D731FFE51269A92EBD1D3992_inline(__this, (bool)0, NULL);
		AscentSim_set_Mode_m7946FC27FD607C87B37DB52807785705728CFDC3_inline(__this, 4, NULL);
		return (bool)1;
	}

IL_0760:
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_289 = __this->___Y;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_290 = __this->___upperMass;
		int32_t L_291;
		L_291 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		NullCheck(L_290);
		int32_t L_292 = L_291;
		double L_293 = (L_290)->GetAt(static_cast<il2cpp_array_size_t>(L_292));
		NullCheck(L_289);
		(L_289)->SetAt(static_cast<il2cpp_array_size_t>(4), (double)L_293);
		List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5* L_294 = __this->___Events;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_295 = (SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9*)il2cpp_codegen_object_new(SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9_il2cpp_TypeInfo_var);
		SimEvent__ctor_m9601BFE450112DDA042CB7CB0DA82C273CCA7367(L_295, NULL);
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_296 = L_295;
		double L_297;
		L_297 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		NullCheck(L_296);
		L_296->___t = L_297;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_298 = L_296;
		NullCheck(L_298);
		L_298->___type = _stringLiteralD991FE6C9AC0B4EDFA368D323F13614F48EBFCB3;
		Il2CppCodeGenWriteBarrier((void**)(&L_298->___type), (void*)_stringLiteralD991FE6C9AC0B4EDFA368D323F13614F48EBFCB3);
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_299 = L_298;
		int32_t L_300;
		L_300 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28 L_301;
		memset((&L_301), 0, sizeof(L_301));
		Nullable_1__ctor_m141FA88563AC0B5179132FB929EABD02C47FF703((&L_301), L_300, Nullable_1__ctor_m141FA88563AC0B5179132FB929EABD02C47FF703_RuntimeMethod_var);
		NullCheck(L_299);
		L_299->___stage = L_301;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_302 = L_299;
		double L_303 = V_6;
		Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 L_304;
		memset((&L_304), 0, sizeof(L_304));
		Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF((&L_304), L_303, Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF_RuntimeMethod_var);
		NullCheck(L_302);
		L_302->___alt = L_304;
		NullCheck(L_294);
		List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_inline(L_294, L_302, List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_RuntimeMethod_var);
		List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5* L_305 = __this->___Events;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_306 = (SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9*)il2cpp_codegen_object_new(SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9_il2cpp_TypeInfo_var);
		SimEvent__ctor_m9601BFE450112DDA042CB7CB0DA82C273CCA7367(L_306, NULL);
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_307 = L_306;
		double L_308;
		L_308 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		NullCheck(L_307);
		L_307->___t = L_308;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_309 = L_307;
		NullCheck(L_309);
		L_309->___type = _stringLiteralC22AD2B06041300DA8ABAAA301A2E02B285863B3;
		Il2CppCodeGenWriteBarrier((void**)(&L_309->___type), (void*)_stringLiteralC22AD2B06041300DA8ABAAA301A2E02B285863B3);
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_310 = L_309;
		int32_t L_311;
		L_311 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28 L_312;
		memset((&L_312), 0, sizeof(L_312));
		Nullable_1__ctor_m141FA88563AC0B5179132FB929EABD02C47FF703((&L_312), ((int32_t)il2cpp_codegen_add(L_311, 1)), Nullable_1__ctor_m141FA88563AC0B5179132FB929EABD02C47FF703_RuntimeMethod_var);
		NullCheck(L_310);
		L_310->___stage = L_312;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_313 = L_310;
		double L_314 = V_6;
		Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 L_315;
		memset((&L_315), 0, sizeof(L_315));
		Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF((&L_315), L_314, Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF_RuntimeMethod_var);
		NullCheck(L_313);
		L_313->___alt = L_315;
		NullCheck(L_305);
		List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_inline(L_305, L_313, List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_RuntimeMethod_var);
		RuntimeObject* L_316;
		L_316 = AscentSim_get_Sink_m34F4D7A8103A12F8CECC0E1F780FA136EC04C37A_inline(__this, NULL);
		if (!L_316)
		{
			goto IL_083f;
		}
	}
	{
		RuntimeObject* L_317;
		L_317 = AscentSim_get_Sink_m34F4D7A8103A12F8CECC0E1F780FA136EC04C37A_inline(__this, NULL);
		int32_t L_318;
		L_318 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 L_319;
		L_319 = AscentSim_Pos_m9934C7D4D6516D15CED891E1F74635B653FF2E8C(__this, NULL);
		FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 L_320;
		L_320 = AscentSim_Vel_m35E2A7777E74DDE08E59164309DA92B57F7E5508(__this, NULL);
		NullCheck(L_317);
		InterfaceActionInvoker3< int32_t, FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7, FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 >::Invoke(3, IFlightSimSink_t6B2E44FEAA2B83A736D6ADA46993E42135AF5A95_il2cpp_TypeInfo_var, L_317, L_318, L_319, L_320);
		RuntimeObject* L_321;
		L_321 = AscentSim_get_Sink_m34F4D7A8103A12F8CECC0E1F780FA136EC04C37A_inline(__this, NULL);
		int32_t L_322;
		L_322 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		double L_323;
		L_323 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		NullCheck(L_321);
		InterfaceActionInvoker2< int32_t, double >::Invoke(0, IFlightSimSink_t6B2E44FEAA2B83A736D6ADA46993E42135AF5A95_il2cpp_TypeInfo_var, L_321, ((int32_t)il2cpp_codegen_add(L_322, 1)), L_323);
	}

IL_083f:
	{
		return (bool)1;
	}

IL_0841:
	{
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_324 = __this->___st;
		int32_t L_325;
		L_325 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		NullCheck(L_324);
		int32_t L_326 = L_325;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_327 = (L_324)->GetAt(static_cast<il2cpp_array_size_t>(L_326));
		NullCheck(L_327);
		double L_328 = L_327->___mdot;
		V_40 = L_328;
		double L_329 = __this->___thrustScale;
		if ((((double)L_329) == ((double)(1.0))))
		{
			goto IL_0871;
		}
	}
	{
		double L_330 = V_40;
		double L_331 = __this->___thrustScale;
		V_40 = ((double)il2cpp_codegen_multiply(L_330, L_331));
	}

IL_0871:
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_332 = __this->___o;
		NullCheck(L_332);
		double L_333 = L_332->___dt;
		double L_334 = V_40;
		if ((((double)L_334) > ((double)(0.0))))
		{
			G_B78_0 = L_333;
			goto IL_0896;
		}
		G_B77_0 = L_333;
	}
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_335 = __this->___o;
		NullCheck(L_335);
		double L_336 = L_335->___dt;
		G_B79_0 = L_336;
		G_B79_1 = G_B77_0;
		goto IL_089b;
	}

IL_0896:
	{
		double L_337 = V_39;
		double L_338 = V_40;
		G_B79_0 = ((double)(L_337/L_338));
		G_B79_1 = G_B78_0;
	}

IL_089b:
	{
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_339;
		L_339 = Math_Min_mA3310F1FF7876DA2FC7F37B822E6DD66410565C1(G_B79_1, G_B79_0, NULL);
		V_16 = L_339;
	}

IL_08a2:
	{
		V_17 = (0.0);
		V_18 = (0.0);
		V_19 = (340.0);
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_340 = __this->___o;
		NullCheck(L_340);
		bool L_341 = L_340->___vacuum;
		if (L_341)
		{
			goto IL_08fb;
		}
	}
	{
		double L_342 = V_6;
		if ((!(((double)L_342) < ((double)(300000.0)))))
		{
			goto IL_08fb;
		}
	}
	{
		double L_343 = V_6;
		AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 L_344;
		L_344 = Atmosphere_At_mA6E038B8FA424E44AD2A97805AC95A264333ECF7(L_343, NULL);
		AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 L_345 = L_344;
		double L_346 = L_345.___p;
		V_17 = L_346;
		AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 L_347 = L_345;
		double L_348 = L_347.___rho;
		V_18 = L_348;
		double L_349 = L_347.___a;
		V_19 = L_349;
	}

IL_08fb:
	{
		double L_350 = V_18;
		double L_351 = V_15;
		double L_352 = V_15;
		V_20 = ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((0.5), L_350)), L_351)), L_352));
		double L_353 = V_20;
		double L_354 = __this->___maxQ;
		if ((!(((double)L_353) > ((double)L_354))))
		{
			goto IL_0937;
		}
	}
	{
		double L_355 = V_20;
		__this->___maxQ = L_355;
		double L_356;
		L_356 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		__this->___maxQTime = L_356;
		double L_357 = V_6;
		__this->___maxQAlt = L_357;
		goto IL_0988;
	}

IL_0937:
	{
		bool L_358 = __this->___maxQPassed;
		if (L_358)
		{
			goto IL_0988;
		}
	}
	{
		double L_359 = __this->___maxQ;
		if ((!(((double)L_359) > ((double)(0.0)))))
		{
			goto IL_0988;
		}
	}
	{
		double L_360 = V_20;
		double L_361 = __this->___maxQ;
		if ((!(((double)L_360) < ((double)((double)il2cpp_codegen_multiply(L_361, (0.97999999999999998)))))))
		{
			goto IL_0988;
		}
	}
	{
		__this->___maxQPassed = (bool)1;
		RuntimeObject* L_362;
		L_362 = AscentSim_get_Sink_m34F4D7A8103A12F8CECC0E1F780FA136EC04C37A_inline(__this, NULL);
		RuntimeObject* L_363 = L_362;
		if (L_363)
		{
			G_B90_0 = L_363;
			goto IL_0977;
		}
		G_B89_0 = L_363;
	}
	{
		goto IL_0988;
	}

IL_0977:
	{
		double L_364 = __this->___maxQ;
		double L_365 = __this->___maxQTime;
		NullCheck(G_B90_0);
		InterfaceActionInvoker2< double, double >::Invoke(2, IFlightSimSink_t6B2E44FEAA2B83A736D6ADA46993E42135AF5A95_il2cpp_TypeInfo_var, G_B90_0, L_364, L_365);
	}

IL_0988:
	{
		double L_366 = V_5;
		double L_367 = V_5;
		V_21 = ((double)((398600441800000.0)/((double)il2cpp_codegen_multiply(L_366, L_367))));
		double L_368 = V_2;
		double L_369 = V_3;
		double L_370;
		L_370 = JsMath_Hypot_mACE54445FD52A26990FD1AD7C43FDC73B6F893D4(L_368, L_369, NULL);
		V_22 = L_370;
		double L_371 = V_17;
		__this->___LastP = L_371;
		double L_372 = V_20;
		__this->___LastQ = L_372;
		double L_373 = V_15;
		double L_374 = V_19;
		__this->___LastMach = ((double)(L_373/L_374));
		__this->___LastThrust = (0.0);
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_375 = __this->___o;
		NullCheck(L_375);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_376 = L_375->___monitor;
		if (!L_376)
		{
			goto IL_0a07;
		}
	}
	{
		double L_377 = V_6;
		double L_378 = V_20;
		double L_379 = V_4;
		double L_380 = V_15;
		double L_381 = V_17;
		double L_382 = V_18;
		double L_383 = V_19;
		double L_384 = V_13;
		double L_385 = V_14;
		double L_386 = V_7;
		double L_387 = V_8;
		double L_388 = V_9;
		double L_389 = V_10;
		double L_390 = V_16;
		bool L_391;
		L_391 = AscentSim_Watch_m57EC47ACCA3B4FA1E4ECA4F5865625DFA39EA697(__this, L_377, L_378, L_379, L_380, L_381, L_382, L_383, L_384, L_385, L_386, L_387, L_388, L_389, L_390, NULL);
		if (!L_391)
		{
			goto IL_0a07;
		}
	}
	{
		double L_392 = V_6;
		double L_393 = V_22;
		AscentSim_Fail_mF30CBB115E75C16458AE75592AA73960EAF8B1F9(__this, L_392, L_393, NULL);
	}

IL_0a07:
	{
		bool L_394;
		L_394 = AscentSim_get_Thrusting_m1221CE066B159B7647B0988CCA625853DB547A22_inline(__this, NULL);
		if (!L_394)
		{
			goto IL_0ba6;
		}
	}
	{
		int32_t L_395;
		L_395 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_396 = __this->___st;
		NullCheck(L_396);
		if ((((int32_t)L_395) >= ((int32_t)((int32_t)(((RuntimeArray*)L_396)->max_length)))))
		{
			goto IL_0ba6;
		}
	}
	{
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_397 = __this->___st;
		int32_t L_398;
		L_398 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		NullCheck(L_397);
		int32_t L_399 = L_398;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_400 = (L_397)->GetAt(static_cast<il2cpp_array_size_t>(L_399));
		V_45 = L_400;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_401 = V_45;
		NullCheck(L_401);
		double L_402 = L_401->___mdot;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_403 = V_45;
		NullCheck(L_403);
		double L_404 = L_403->___ispSL;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_405 = V_45;
		NullCheck(L_405);
		double L_406 = L_405->___ispVac;
		double L_407 = V_17;
		double L_408;
		L_408 = RocketEq_IspAt_m4FFE489F4D4A8AD0D4199B175E2B729E0727C9B6(L_404, L_406, L_407, NULL);
		V_46 = ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_402, (9.8066499999999994))), L_408));
		double L_409 = __this->___thrustScale;
		if ((((double)L_409) == ((double)(1.0))))
		{
			goto IL_0a79;
		}
	}
	{
		double L_410 = V_46;
		double L_411 = __this->___thrustScale;
		V_46 = ((double)il2cpp_codegen_multiply(L_410, L_411));
	}

IL_0a79:
	{
		double L_412 = V_46;
		__this->___LastThrust = L_412;
		double L_413 = V_46;
		double L_414 = V_4;
		V_47 = ((double)(L_413/L_414));
		double L_415 = __this->___dvExp;
		double L_416 = V_47;
		double L_417 = V_16;
		__this->___dvExp = ((double)il2cpp_codegen_add(L_415, ((double)il2cpp_codegen_multiply(L_416, L_417))));
		int32_t L_418;
		L_418 = AscentSim_get_Mode_m5522E937FEF440F09E1F28A50A534066FEA2E2C9_inline(__this, NULL);
		if ((!(((uint32_t)L_418) == ((uint32_t)2))))
		{
			goto IL_0ac0;
		}
	}
	{
		double L_419 = V_15;
		if ((!(((double)L_419) > ((double)(1.0)))))
		{
			goto IL_0ac0;
		}
	}
	{
		double L_420 = V_13;
		double L_421 = V_15;
		V_48 = ((double)(L_420/L_421));
		double L_422 = V_14;
		double L_423 = V_15;
		V_49 = ((double)(L_422/L_423));
		goto IL_0aef;
	}

IL_0ac0:
	{
		double L_424;
		L_424 = AscentSim_get_PitchCmd_mC76BA7E009D9C12D9F966E247CC7909311A135ED_inline(__this, NULL);
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_425;
		L_425 = cos(L_424);
		double L_426;
		L_426 = AscentSim_get_PitchCmd_mC76BA7E009D9C12D9F966E247CC7909311A135ED_inline(__this, NULL);
		double L_427;
		L_427 = sin(L_426);
		V_53 = L_427;
		double L_428 = L_425;
		double L_429 = V_9;
		double L_430 = V_53;
		double L_431 = V_7;
		V_48 = ((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_428, L_429)), ((double)il2cpp_codegen_multiply(L_430, L_431))));
		double L_432 = V_10;
		double L_433 = V_53;
		double L_434 = V_8;
		V_49 = ((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_428, L_432)), ((double)il2cpp_codegen_multiply(L_433, L_434))));
	}

IL_0aef:
	{
		double L_435 = V_22;
		if ((((double)L_435) > ((double)(1.0))))
		{
			goto IL_0b07;
		}
	}
	{
		G_B105_0 = (0.0);
		goto IL_0b13;
	}

IL_0b07:
	{
		double L_436 = V_48;
		double L_437 = V_2;
		double L_438 = V_49;
		double L_439 = V_3;
		double L_440 = V_22;
		G_B105_0 = ((double)(((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_436, L_437)), ((double)il2cpp_codegen_multiply(L_438, L_439))))/L_440));
	}

IL_0b13:
	{
		V_50 = G_B105_0;
		double L_441 = V_18;
		if ((((double)L_441) > ((double)(0.0))))
		{
			goto IL_0b2d;
		}
	}
	{
		G_B108_0 = (0.0);
		goto IL_0b5c;
	}

IL_0b2d:
	{
		double L_442 = V_18;
		double L_443 = V_15;
		double L_444 = V_15;
		double L_445 = V_15;
		double L_446 = V_19;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_447 = V_45;
		NullCheck(L_447);
		double L_448 = L_447->___cd;
		double L_449;
		L_449 = Aero_CdMach_m0F78C3ADE3138A85FBDF51DC71A3040C035EED47(((double)(L_445/L_446)), L_448, NULL);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_450 = V_45;
		NullCheck(L_450);
		double L_451 = L_450->___area;
		double L_452 = V_4;
		G_B108_0 = ((double)(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((0.5), L_442)), L_443)), L_444)), L_449)), L_451))/L_452));
	}

IL_0b5c:
	{
		V_51 = G_B108_0;
		double* L_453 = (double*)(&__this->___lossG);
		double* L_454 = (double*)(&__this->___lossD);
		double* L_455 = (double*)(&__this->___lossS);
		double L_456 = V_21;
		double L_457 = V_11;
		double L_458 = V_22;
		double L_459 = V_47;
		double L_460 = V_50;
		double L_461 = V_51;
		double L_462 = V_16;
		LossAccount_Add_m962FF29EAAF2D6352E3DAC40436A0A57FDFAB859(L_453, L_454, L_455, L_456, L_457, L_458, L_459, L_460, L_461, L_462, NULL);
		double L_463 = V_47;
		double L_464 = V_51;
		V_52 = ((double)(((double)il2cpp_codegen_subtract(L_463, L_464))/(9.8066499999999994)));
		double L_465 = V_52;
		double L_466 = __this->___maxG;
		if ((!(((double)L_465) > ((double)L_466))))
		{
			goto IL_0ba6;
		}
	}
	{
		double L_467 = V_52;
		__this->___maxG = L_467;
	}

IL_0ba6:
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_468 = __this->___o;
		NullCheck(L_468);
		double L_469 = L_468->___sampleEvery;
		if ((!(((double)L_469) > ((double)(0.0)))))
		{
			goto IL_0c60;
		}
	}
	{
		double L_470;
		L_470 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		double L_471 = __this->___nextSample;
		if ((!(((double)L_470) >= ((double)L_471))))
		{
			goto IL_0c60;
		}
	}
	{
		List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8* L_472 = __this->___Samples;
		il2cpp_codegen_initobj((&V_54), sizeof(SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121));
		double L_473;
		L_473 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		(&V_54)->___t = L_473;
		double L_474 = V_6;
		(&V_54)->___alt = L_474;
		double L_475 = V_22;
		(&V_54)->___v = L_475;
		double L_476 = V_15;
		(&V_54)->___vrel = L_476;
		double L_477 = V_20;
		(&V_54)->___q = L_477;
		double L_478 = V_4;
		(&V_54)->___m = L_478;
		int32_t L_479;
		L_479 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		(&V_54)->___stage = ((int32_t)il2cpp_codegen_add(L_479, 1));
		double L_480;
		L_480 = AscentSim_get_PitchCmd_mC76BA7E009D9C12D9F966E247CC7909311A135ED_inline(__this, NULL);
		(&V_54)->___pitch = L_480;
		int32_t L_481;
		L_481 = AscentSim_get_Mode_m5522E937FEF440F09E1F28A50A534066FEA2E2C9_inline(__this, NULL);
		(&V_54)->___mode = L_481;
		SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121 L_482 = V_54;
		NullCheck(L_472);
		List_1_Add_m67EF1200FCD49355EECE77EB754C2EED52F8B3DE_inline(L_472, L_482, List_1_Add_m67EF1200FCD49355EECE77EB754C2EED52F8B3DE_RuntimeMethod_var);
		double L_483 = __this->___nextSample;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_484 = __this->___o;
		NullCheck(L_484);
		double L_485 = L_484->___sampleEvery;
		__this->___nextSample = ((double)il2cpp_codegen_add(L_483, L_485));
	}

IL_0c60:
	{
		RK4_t1BC6735398DED08AF0675CEEB7983FDA7AB140CB* L_486 = __this->___rk;
		Deriv_t32B789F19EA5F7301FEE48A702C2B32AF64EF69A* L_487 = __this->___deriv;
		double L_488;
		L_488 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_489 = __this->___Y;
		double L_490 = V_16;
		NullCheck(L_486);
		RK4_Step_mA370B9F53F0B0CD1473EFDED893537324B37C9A2(L_486, L_487, L_488, L_489, L_490, NULL);
		double L_491;
		L_491 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		double L_492 = V_16;
		AscentSim_set_T_mECCDAC4D95A8329E97710AAC1A222166080AE6F5_inline(__this, ((double)il2cpp_codegen_add(L_491, L_492)), NULL);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_493 = __this->___Y;
		NullCheck(L_493);
		int32_t L_494 = 0;
		double L_495 = (L_493)->GetAt(static_cast<il2cpp_array_size_t>(L_494));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_496 = __this->___Y;
		NullCheck(L_496);
		int32_t L_497 = 1;
		double L_498 = (L_496)->GetAt(static_cast<il2cpp_array_size_t>(L_497));
		double L_499;
		L_499 = JsMath_Hypot_mACE54445FD52A26990FD1AD7C43FDC73B6F893D4(L_495, L_498, NULL);
		V_23 = L_499;
		double L_500 = V_23;
		V_24 = ((double)il2cpp_codegen_subtract(L_500, (6378137.0)));
		bool L_501 = __this->___liftedOff;
		if (L_501)
		{
			goto IL_0d13;
		}
	}
	{
		double L_502 = V_24;
		if ((!(((double)L_502) > ((double)(1.0)))))
		{
			goto IL_0d13;
		}
	}
	{
		__this->___liftedOff = (bool)1;
		List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5* L_503 = __this->___Events;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_504 = (SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9*)il2cpp_codegen_object_new(SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9_il2cpp_TypeInfo_var);
		SimEvent__ctor_m9601BFE450112DDA042CB7CB0DA82C273CCA7367(L_504, NULL);
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_505 = L_504;
		double L_506;
		L_506 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		NullCheck(L_505);
		L_505->___t = L_506;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_507 = L_505;
		NullCheck(L_507);
		L_507->___type = _stringLiteral48DF3B432B8D62D5F8D80DE53D4D9FCC446CD284;
		Il2CppCodeGenWriteBarrier((void**)(&L_507->___type), (void*)_stringLiteral48DF3B432B8D62D5F8D80DE53D4D9FCC446CD284);
		NullCheck(L_503);
		List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_inline(L_503, L_507, List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_RuntimeMethod_var);
		RuntimeObject* L_508;
		L_508 = AscentSim_get_Sink_m34F4D7A8103A12F8CECC0E1F780FA136EC04C37A_inline(__this, NULL);
		RuntimeObject* L_509 = L_508;
		if (L_509)
		{
			G_B117_0 = L_509;
			goto IL_0d02;
		}
		G_B116_0 = L_509;
	}
	{
		goto IL_0d13;
	}

IL_0d02:
	{
		double L_510;
		L_510 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 L_511 = __this->___pad;
		NullCheck(G_B117_0);
		InterfaceActionInvoker2< double, FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 >::Invoke(1, IFlightSimSink_t6B2E44FEAA2B83A736D6ADA46993E42135AF5A95_il2cpp_TypeInfo_var, G_B117_0, L_510, L_511);
	}

IL_0d13:
	{
		bool L_512 = __this->___sawKarman;
		if (L_512)
		{
			goto IL_0d3b;
		}
	}
	{
		double L_513 = V_24;
		if ((!(((double)L_513) >= ((double)(100000.0)))))
		{
			goto IL_0d3b;
		}
	}
	{
		__this->___sawKarman = (bool)1;
		double L_514;
		L_514 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		__this->___karmanT = L_514;
	}

IL_0d3b:
	{
		bool L_515 = __this->___fairingSent;
		if (L_515)
		{
			goto IL_0dfe;
		}
	}
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_516 = __this->___o;
		NullCheck(L_516);
		double L_517 = L_516->___fairingJettisonAltM;
		if ((!(((double)L_517) > ((double)(0.0)))))
		{
			goto IL_0dfe;
		}
	}
	{
		bool L_518 = __this->___maxQPassed;
		if (!L_518)
		{
			goto IL_0dfe;
		}
	}
	{
		double L_519 = V_24;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_520 = __this->___o;
		NullCheck(L_520);
		double L_521 = L_520->___fairingJettisonAltM;
		if ((!(((double)L_519) >= ((double)L_521))))
		{
			goto IL_0dfe;
		}
	}
	{
		__this->___fairingSent = (bool)1;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_522 = __this->___Y;
		NullCheck(L_522);
		int32_t L_523 = 2;
		double L_524 = (L_522)->GetAt(static_cast<il2cpp_array_size_t>(L_523));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_525 = __this->___Y;
		NullCheck(L_525);
		int32_t L_526 = 3;
		double L_527 = (L_525)->GetAt(static_cast<il2cpp_array_size_t>(L_526));
		double L_528;
		L_528 = JsMath_Hypot_mACE54445FD52A26990FD1AD7C43FDC73B6F893D4(L_524, L_527, NULL);
		V_55 = L_528;
		List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5* L_529 = __this->___Events;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_530 = (SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9*)il2cpp_codegen_object_new(SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9_il2cpp_TypeInfo_var);
		SimEvent__ctor_m9601BFE450112DDA042CB7CB0DA82C273CCA7367(L_530, NULL);
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_531 = L_530;
		double L_532;
		L_532 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		NullCheck(L_531);
		L_531->___t = L_532;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_533 = L_531;
		NullCheck(L_533);
		L_533->___type = _stringLiteralADFC4849E68D1978E71A9D4530D530AB1F600C39;
		Il2CppCodeGenWriteBarrier((void**)(&L_533->___type), (void*)_stringLiteralADFC4849E68D1978E71A9D4530D530AB1F600C39);
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_534 = L_533;
		double L_535 = V_24;
		Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 L_536;
		memset((&L_536), 0, sizeof(L_536));
		Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF((&L_536), L_535, Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF_RuntimeMethod_var);
		NullCheck(L_534);
		L_534->___alt = L_536;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_537 = L_534;
		double L_538 = V_55;
		Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 L_539;
		memset((&L_539), 0, sizeof(L_539));
		Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF((&L_539), L_538, Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF_RuntimeMethod_var);
		NullCheck(L_537);
		L_537->___v = L_539;
		NullCheck(L_529);
		List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_inline(L_529, L_537, List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_RuntimeMethod_var);
		RuntimeObject* L_540;
		L_540 = AscentSim_get_Sink_m34F4D7A8103A12F8CECC0E1F780FA136EC04C37A_inline(__this, NULL);
		RuntimeObject* L_541 = L_540;
		if (L_541)
		{
			G_B127_0 = L_541;
			goto IL_0de7;
		}
		G_B126_0 = L_541;
	}
	{
		goto IL_0dfe;
	}

IL_0de7:
	{
		double L_542;
		L_542 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 L_543;
		L_543 = AscentSim_Pos_m9934C7D4D6516D15CED891E1F74635B653FF2E8C(__this, NULL);
		FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 L_544;
		L_544 = AscentSim_Vel_m35E2A7777E74DDE08E59164309DA92B57F7E5508(__this, NULL);
		NullCheck(G_B127_0);
		InterfaceActionInvoker3< double, FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7, FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 >::Invoke(4, IFlightSimSink_t6B2E44FEAA2B83A736D6ADA46993E42135AF5A95_il2cpp_TypeInfo_var, G_B127_0, L_542, L_543, L_544);
	}

IL_0dfe:
	{
		double L_545 = V_24;
		if ((!(((double)L_545) < ((double)(0.0)))))
		{
			goto IL_0f7b;
		}
	}
	{
		bool L_546 = __this->___liftedOff;
		if (L_546)
		{
			goto IL_0e62;
		}
	}
	{
		bool L_547;
		L_547 = AscentSim_get_Thrusting_m1221CE066B159B7647B0988CCA625853DB547A22_inline(__this, NULL);
		if (!L_547)
		{
			goto IL_0e62;
		}
	}
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_548 = __this->___Y;
		NullCheck(L_548);
		(L_548)->SetAt(static_cast<il2cpp_array_size_t>(0), (double)(0.0));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_549 = __this->___Y;
		NullCheck(L_549);
		(L_549)->SetAt(static_cast<il2cpp_array_size_t>(1), (double)(6378137.0));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_550 = __this->___Y;
		double L_551 = __this->___vSite;
		NullCheck(L_550);
		(L_550)->SetAt(static_cast<il2cpp_array_size_t>(2), (double)((-L_551)));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_552 = __this->___Y;
		NullCheck(L_552);
		(L_552)->SetAt(static_cast<il2cpp_array_size_t>(3), (double)(0.0));
		return (bool)1;
	}

IL_0e62:
	{
		double L_553 = V_23;
		if ((((double)L_553) > ((double)(0.0))))
		{
			goto IL_0e7a;
		}
	}
	{
		G_B135_0 = (0.0);
		goto IL_0ea1;
	}

IL_0e7a:
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_554 = __this->___Y;
		NullCheck(L_554);
		int32_t L_555 = 2;
		double L_556 = (L_554)->GetAt(static_cast<il2cpp_array_size_t>(L_555));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_557 = __this->___Y;
		NullCheck(L_557);
		int32_t L_558 = 0;
		double L_559 = (L_557)->GetAt(static_cast<il2cpp_array_size_t>(L_558));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_560 = __this->___Y;
		NullCheck(L_560);
		int32_t L_561 = 3;
		double L_562 = (L_560)->GetAt(static_cast<il2cpp_array_size_t>(L_561));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_563 = __this->___Y;
		NullCheck(L_563);
		int32_t L_564 = 1;
		double L_565 = (L_563)->GetAt(static_cast<il2cpp_array_size_t>(L_564));
		double L_566 = V_23;
		G_B135_0 = ((double)(((-((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_556, L_559)), ((double)il2cpp_codegen_multiply(L_562, L_565))))))/L_566));
	}

IL_0ea1:
	{
		V_56 = G_B135_0;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_567 = __this->___o;
		NullCheck(L_567);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_568 = L_567->___monitor;
		if (!L_568)
		{
			goto IL_0eed;
		}
	}
	{
		String_t* L_569 = __this->___outcome;
		bool L_570;
		L_570 = String_op_Inequality_m8C940F3CFC42866709D7CA931B3D77B4BE94BCB6(L_569, _stringLiteral1E3DB61522C4E7AA1D5A216FFA2CA92BAFA2EBF1, NULL);
		if (!L_570)
		{
			goto IL_0eed;
		}
	}
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_571 = __this->___o;
		NullCheck(L_571);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_572 = L_571->___monitor;
		double L_573;
		L_573 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		double L_574 = V_56;
		NullCheck(L_572);
		bool L_575;
		L_575 = FailureMonitor_OnImpact_m66B964FF0BF29F5461731342F7087CE498B96415(L_572, L_573, L_574, NULL);
		if (!L_575)
		{
			goto IL_0eed;
		}
	}
	{
		double L_576 = V_56;
		AscentSim_Fail_mF30CBB115E75C16458AE75592AA73960EAF8B1F9(__this, (0.0), L_576, NULL);
	}

IL_0eed:
	{
		String_t* L_577 = __this->___outcome;
		bool L_578;
		L_578 = String_op_Inequality_m8C940F3CFC42866709D7CA931B3D77B4BE94BCB6(L_577, _stringLiteral1E3DB61522C4E7AA1D5A216FFA2CA92BAFA2EBF1, NULL);
		if (!L_578)
		{
			goto IL_0f0a;
		}
	}
	{
		__this->___outcome = _stringLiteral88CE3FCD887A58F8A5764032FA39B0C1CD48440B;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___outcome), (void*)_stringLiteral88CE3FCD887A58F8A5764032FA39B0C1CD48440B);
	}

IL_0f0a:
	{
		List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5* L_579 = __this->___Events;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_580 = (SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9*)il2cpp_codegen_object_new(SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9_il2cpp_TypeInfo_var);
		SimEvent__ctor_m9601BFE450112DDA042CB7CB0DA82C273CCA7367(L_580, NULL);
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_581 = L_580;
		double L_582;
		L_582 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		NullCheck(L_581);
		L_581->___t = L_582;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_583 = L_581;
		NullCheck(L_583);
		L_583->___type = _stringLiteralCDE83734553D6AC0E1530734343508D1FC27A13F;
		Il2CppCodeGenWriteBarrier((void**)(&L_583->___type), (void*)_stringLiteralCDE83734553D6AC0E1530734343508D1FC27A13F);
		NullCheck(L_579);
		List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_inline(L_579, L_583, List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_RuntimeMethod_var);
		double L_584 = V_56;
		if ((((double)L_584) > ((double)(0.0))))
		{
			goto IL_0f55;
		}
	}
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_585 = __this->___Y;
		NullCheck(L_585);
		int32_t L_586 = 2;
		double L_587 = (L_585)->GetAt(static_cast<il2cpp_array_size_t>(L_586));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_588 = __this->___Y;
		NullCheck(L_588);
		int32_t L_589 = 3;
		double L_590 = (L_588)->GetAt(static_cast<il2cpp_array_size_t>(L_589));
		double L_591;
		L_591 = JsMath_Hypot_mACE54445FD52A26990FD1AD7C43FDC73B6F893D4(L_587, L_590, NULL);
		G_B144_0 = L_591;
		goto IL_0f57;
	}

IL_0f55:
	{
		double L_592 = V_56;
		G_B144_0 = L_592;
	}

IL_0f57:
	{
		V_57 = G_B144_0;
		RuntimeObject* L_593;
		L_593 = AscentSim_get_Sink_m34F4D7A8103A12F8CECC0E1F780FA136EC04C37A_inline(__this, NULL);
		RuntimeObject* L_594 = L_593;
		if (L_594)
		{
			G_B146_0 = L_594;
			goto IL_0f65;
		}
		G_B145_0 = L_594;
	}
	{
		goto IL_0f72;
	}

IL_0f65:
	{
		double L_595 = V_57;
		FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 L_596;
		L_596 = AscentSim_Pos_m9934C7D4D6516D15CED891E1F74635B653FF2E8C(__this, NULL);
		NullCheck(G_B146_0);
		InterfaceActionInvoker2< double, FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 >::Invoke(10, IFlightSimSink_t6B2E44FEAA2B83A736D6ADA46993E42135AF5A95_il2cpp_TypeInfo_var, G_B146_0, L_595, L_596);
	}

IL_0f72:
	{
		AscentSim_set_Done_m95D7993A7D005264297DDDDF34649C09515E702C_inline(__this, (bool)1, NULL);
		return (bool)0;
	}

IL_0f7b:
	{
		bool L_597;
		L_597 = AscentSim_get_Thrusting_m1221CE066B159B7647B0988CCA625853DB547A22_inline(__this, NULL);
		if (!L_597)
		{
			goto IL_105e;
		}
	}
	{
		int32_t L_598;
		L_598 = AscentSim_get_Mode_m5522E937FEF440F09E1F28A50A534066FEA2E2C9_inline(__this, NULL);
		if ((!(((uint32_t)L_598) == ((uint32_t)3))))
		{
			goto IL_105e;
		}
	}
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_599 = __this->___o;
		NullCheck(L_599);
		bool L_600 = L_599->___cutoff;
		if (!L_600)
		{
			goto IL_105e;
		}
	}
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_601 = __this->___Y;
		NullCheck(L_601);
		int32_t L_602 = 0;
		double L_603 = (L_601)->GetAt(static_cast<il2cpp_array_size_t>(L_602));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_604 = __this->___Y;
		NullCheck(L_604);
		int32_t L_605 = 1;
		double L_606 = (L_604)->GetAt(static_cast<il2cpp_array_size_t>(L_605));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_607 = __this->___Y;
		NullCheck(L_607);
		int32_t L_608 = 2;
		double L_609 = (L_607)->GetAt(static_cast<il2cpp_array_size_t>(L_608));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_610 = __this->___Y;
		NullCheck(L_610);
		int32_t L_611 = 3;
		double L_612 = (L_610)->GetAt(static_cast<il2cpp_array_size_t>(L_611));
		OrbitElements_t5EEC91C4BE0D34654FC26DBE704E101E14BB7D98 L_613;
		L_613 = Orbit_Planar_m81894386C1A7EA3C3C48E177AAAF4946DBF669E7(L_603, L_606, L_609, L_612, (398600441800000.0), NULL);
		V_58 = L_613;
		OrbitElements_t5EEC91C4BE0D34654FC26DBE704E101E14BB7D98 L_614 = V_58;
		double L_615 = L_614.___periAlt;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_616 = __this->___o;
		NullCheck(L_616);
		double L_617 = L_616->___targetAlt;
		double L_618 = V_24;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_619;
		L_619 = Math_Min_mA3310F1FF7876DA2FC7F37B822E6DD66410565C1(L_617, L_618, NULL);
		if ((!(((double)L_615) >= ((double)((double)il2cpp_codegen_subtract(L_619, (10000.0)))))))
		{
			goto IL_105e;
		}
	}
	{
		OrbitElements_t5EEC91C4BE0D34654FC26DBE704E101E14BB7D98 L_620 = V_58;
		double L_621 = L_620.___e;
		if ((!(((double)L_621) < ((double)(1.0)))))
		{
			goto IL_105e;
		}
	}
	{
		AscentSim_set_Thrusting_mD9D054E78DF826C6D731FFE51269A92EBD1D3992_inline(__this, (bool)0, NULL);
		AscentSim_set_Mode_m7946FC27FD607C87B37DB52807785705728CFDC3_inline(__this, 4, NULL);
		List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5* L_622 = __this->___Events;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_623 = (SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9*)il2cpp_codegen_object_new(SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9_il2cpp_TypeInfo_var);
		SimEvent__ctor_m9601BFE450112DDA042CB7CB0DA82C273CCA7367(L_623, NULL);
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_624 = L_623;
		double L_625;
		L_625 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		NullCheck(L_624);
		L_624->___t = L_625;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_626 = L_624;
		NullCheck(L_626);
		L_626->___type = _stringLiteralAD88ECBD135D7E7861EE52F538BEC1F9767085EA;
		Il2CppCodeGenWriteBarrier((void**)(&L_626->___type), (void*)_stringLiteralAD88ECBD135D7E7861EE52F538BEC1F9767085EA);
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_627 = L_626;
		int32_t L_628;
		L_628 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28 L_629;
		memset((&L_629), 0, sizeof(L_629));
		Nullable_1__ctor_m141FA88563AC0B5179132FB929EABD02C47FF703((&L_629), ((int32_t)il2cpp_codegen_add(L_628, 1)), Nullable_1__ctor_m141FA88563AC0B5179132FB929EABD02C47FF703_RuntimeMethod_var);
		NullCheck(L_627);
		L_627->___stage = L_629;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_630 = L_627;
		double L_631 = V_24;
		Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 L_632;
		memset((&L_632), 0, sizeof(L_632));
		Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF((&L_632), L_631, Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF_RuntimeMethod_var);
		NullCheck(L_630);
		L_630->___alt = L_632;
		NullCheck(L_622);
		List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_inline(L_622, L_630, List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_RuntimeMethod_var);
	}

IL_105e:
	{
		bool L_633;
		L_633 = AscentSim_get_Thrusting_m1221CE066B159B7647B0988CCA625853DB547A22_inline(__this, NULL);
		if (L_633)
		{
			goto IL_1081;
		}
	}
	{
		String_t* L_634 = __this->___outcome;
		bool L_635;
		L_635 = String_op_Inequality_m8C940F3CFC42866709D7CA931B3D77B4BE94BCB6(L_634, _stringLiteral1E3DB61522C4E7AA1D5A216FFA2CA92BAFA2EBF1, NULL);
		if (!L_635)
		{
			goto IL_1081;
		}
	}
	{
		AscentSim_set_Done_m95D7993A7D005264297DDDDF34649C09515E702C_inline(__this, (bool)1, NULL);
		return (bool)0;
	}

IL_1081:
	{
		return (bool)1;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void AscentSim_BreakUp_m0F94CFA8E45D103D7AAD29920AE79CE1AA8A42A1 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* ___0_ev, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral1E3DB61522C4E7AA1D5A216FFA2CA92BAFA2EBF1);
		s_Il2CppMethodInitialized = true;
	}
	double V_0 = 0.0;
	{
		AscentSim_EnsureIgnition_mD7BE0F38DF384B744ACA8D0BF38AE349D6149F01(__this, NULL);
		FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* L_0 = ___0_ev;
		if (!L_0)
		{
			goto IL_0040;
		}
	}
	{
		FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* L_1 = ___0_ev;
		__this->___playedBreak = L_1;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___playedBreak), (void*)L_1);
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_2 = __this->___o;
		NullCheck(L_2);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_3 = L_2->___monitor;
		if (!L_3)
		{
			goto IL_0040;
		}
	}
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_4 = __this->___o;
		NullCheck(L_4);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_5 = L_4->___monitor;
		NullCheck(L_5);
		FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* L_6 = L_5->___Event;
		if (L_6)
		{
			goto IL_0040;
		}
	}
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_7 = __this->___o;
		NullCheck(L_7);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_8 = L_7->___monitor;
		FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* L_9 = ___0_ev;
		NullCheck(L_8);
		L_8->___Event = L_9;
		Il2CppCodeGenWriteBarrier((void**)(&L_8->___Event), (void*)L_9);
	}

IL_0040:
	{
		String_t* L_10 = __this->___outcome;
		bool L_11;
		L_11 = String_op_Inequality_m8C940F3CFC42866709D7CA931B3D77B4BE94BCB6(L_10, _stringLiteral1E3DB61522C4E7AA1D5A216FFA2CA92BAFA2EBF1, NULL);
		if (!L_11)
		{
			goto IL_008e;
		}
	}
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_12 = __this->___Y;
		NullCheck(L_12);
		int32_t L_13 = 0;
		double L_14 = (L_12)->GetAt(static_cast<il2cpp_array_size_t>(L_13));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_15 = __this->___Y;
		NullCheck(L_15);
		int32_t L_16 = 1;
		double L_17 = (L_15)->GetAt(static_cast<il2cpp_array_size_t>(L_16));
		double L_18;
		L_18 = JsMath_Hypot_mACE54445FD52A26990FD1AD7C43FDC73B6F893D4(L_14, L_17, NULL);
		V_0 = L_18;
		double L_19 = V_0;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_20 = __this->___Y;
		NullCheck(L_20);
		int32_t L_21 = 2;
		double L_22 = (L_20)->GetAt(static_cast<il2cpp_array_size_t>(L_21));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_23 = __this->___Y;
		NullCheck(L_23);
		int32_t L_24 = 3;
		double L_25 = (L_23)->GetAt(static_cast<il2cpp_array_size_t>(L_24));
		double L_26;
		L_26 = JsMath_Hypot_mACE54445FD52A26990FD1AD7C43FDC73B6F893D4(L_22, L_25, NULL);
		AscentSim_Fail_mF30CBB115E75C16458AE75592AA73960EAF8B1F9(__this, ((double)il2cpp_codegen_subtract(L_19, (6378137.0))), L_26, NULL);
	}

IL_008e:
	{
		AscentSim_set_Done_m95D7993A7D005264297DDDDF34649C09515E702C_inline(__this, (bool)1, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void AscentSim_Fail_mF30CBB115E75C16458AE75592AA73960EAF8B1F9 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, double ___0_alt, double ___1_speed, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IFlightSimSink_t6B2E44FEAA2B83A736D6ADA46993E42135AF5A95_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Nullable_1__ctor_m141FA88563AC0B5179132FB929EABD02C47FF703_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral1E3DB61522C4E7AA1D5A216FFA2CA92BAFA2EBF1);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralDA39A3EE5E6B4B0D3255BFEF95601890AFD80709);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* V_1 = NULL;
	String_t* V_2 = NULL;
	String_t* V_3 = NULL;
	int32_t V_4 = 0;
	int32_t G_B5_0 = 0;
	FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* G_B10_0 = NULL;
	String_t* G_B16_0 = NULL;
	String_t* G_B20_0 = NULL;
	int32_t G_B23_0 = 0;
	{
		String_t* L_0 = __this->___outcome;
		bool L_1;
		L_1 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_0, _stringLiteral1E3DB61522C4E7AA1D5A216FFA2CA92BAFA2EBF1, NULL);
		if (!L_1)
		{
			goto IL_0013;
		}
	}
	{
		return;
	}

IL_0013:
	{
		__this->___outcome = _stringLiteral1E3DB61522C4E7AA1D5A216FFA2CA92BAFA2EBF1;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___outcome), (void*)_stringLiteral1E3DB61522C4E7AA1D5A216FFA2CA92BAFA2EBF1);
		AscentSim_set_Thrusting_mD9D054E78DF826C6D731FFE51269A92EBD1D3992_inline(__this, (bool)0, NULL);
		__this->___thrustScale = (0.0);
		AscentSim_set_Mode_m7946FC27FD607C87B37DB52807785705728CFDC3_inline(__this, 4, NULL);
		int32_t L_2;
		L_2 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_3 = __this->___st;
		NullCheck(L_3);
		if ((((int32_t)L_2) < ((int32_t)((int32_t)(((RuntimeArray*)L_3)->max_length)))))
		{
			goto IL_0055;
		}
	}
	{
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_4 = __this->___st;
		NullCheck(L_4);
		G_B5_0 = ((int32_t)(((RuntimeArray*)L_4)->max_length));
		goto IL_005d;
	}

IL_0055:
	{
		int32_t L_5;
		L_5 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		G_B5_0 = ((int32_t)il2cpp_codegen_add(L_5, 1));
	}

IL_005d:
	{
		V_0 = G_B5_0;
		List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5* L_6 = __this->___Events;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_7 = (SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9*)il2cpp_codegen_object_new(SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9_il2cpp_TypeInfo_var);
		SimEvent__ctor_m9601BFE450112DDA042CB7CB0DA82C273CCA7367(L_7, NULL);
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_8 = L_7;
		double L_9;
		L_9 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		NullCheck(L_8);
		L_8->___t = L_9;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_10 = L_8;
		NullCheck(L_10);
		L_10->___type = _stringLiteral1E3DB61522C4E7AA1D5A216FFA2CA92BAFA2EBF1;
		Il2CppCodeGenWriteBarrier((void**)(&L_10->___type), (void*)_stringLiteral1E3DB61522C4E7AA1D5A216FFA2CA92BAFA2EBF1);
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_11 = L_10;
		int32_t L_12 = V_0;
		Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28 L_13;
		memset((&L_13), 0, sizeof(L_13));
		Nullable_1__ctor_m141FA88563AC0B5179132FB929EABD02C47FF703((&L_13), L_12, Nullable_1__ctor_m141FA88563AC0B5179132FB929EABD02C47FF703_RuntimeMethod_var);
		NullCheck(L_11);
		L_11->___stage = L_13;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_14 = L_11;
		double L_15 = ___0_alt;
		Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 L_16;
		memset((&L_16), 0, sizeof(L_16));
		Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF((&L_16), L_15, Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF_RuntimeMethod_var);
		NullCheck(L_14);
		L_14->___alt = L_16;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_17 = L_14;
		double L_18 = ___1_speed;
		Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 L_19;
		memset((&L_19), 0, sizeof(L_19));
		Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF((&L_19), L_18, Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF_RuntimeMethod_var);
		NullCheck(L_17);
		L_17->___v = L_19;
		NullCheck(L_6);
		List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_inline(L_6, L_17, List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_RuntimeMethod_var);
		RuntimeObject* L_20;
		L_20 = AscentSim_get_Sink_m34F4D7A8103A12F8CECC0E1F780FA136EC04C37A_inline(__this, NULL);
		if (L_20)
		{
			goto IL_00b2;
		}
	}
	{
		return;
	}

IL_00b2:
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_21 = __this->___o;
		NullCheck(L_21);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_22 = L_21->___monitor;
		if (L_22)
		{
			goto IL_00c2;
		}
	}
	{
		G_B10_0 = ((FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57*)(NULL));
		goto IL_00d2;
	}

IL_00c2:
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_23 = __this->___o;
		NullCheck(L_23);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_24 = L_23->___monitor;
		NullCheck(L_24);
		FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* L_25 = L_24->___Event;
		G_B10_0 = L_25;
	}

IL_00d2:
	{
		V_1 = G_B10_0;
		FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* L_26 = V_1;
		if (L_26)
		{
			goto IL_00dd;
		}
	}
	{
		FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* L_27 = __this->___playedBreak;
		V_1 = L_27;
	}

IL_00dd:
	{
		FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* L_28 = V_1;
		if (!L_28)
		{
			goto IL_00e8;
		}
	}
	{
		FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* L_29 = V_1;
		NullCheck(L_29);
		String_t* L_30 = L_29->___kind;
		if (L_30)
		{
			goto IL_00ef;
		}
	}

IL_00e8:
	{
		G_B16_0 = _stringLiteral1E3DB61522C4E7AA1D5A216FFA2CA92BAFA2EBF1;
		goto IL_00f5;
	}

IL_00ef:
	{
		FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* L_31 = V_1;
		NullCheck(L_31);
		String_t* L_32 = L_31->___kind;
		G_B16_0 = L_32;
	}

IL_00f5:
	{
		V_2 = G_B16_0;
		FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* L_33 = V_1;
		if (!L_33)
		{
			goto IL_0101;
		}
	}
	{
		FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* L_34 = V_1;
		NullCheck(L_34);
		String_t* L_35 = L_34->___cause;
		if (L_35)
		{
			goto IL_0108;
		}
	}

IL_0101:
	{
		G_B20_0 = _stringLiteralDA39A3EE5E6B4B0D3255BFEF95601890AFD80709;
		goto IL_010e;
	}

IL_0108:
	{
		FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* L_36 = V_1;
		NullCheck(L_36);
		String_t* L_37 = L_36->___cause;
		G_B20_0 = L_37;
	}

IL_010e:
	{
		V_3 = G_B20_0;
		FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* L_38 = V_1;
		if (L_38)
		{
			goto IL_0115;
		}
	}
	{
		G_B23_0 = (-1);
		goto IL_011b;
	}

IL_0115:
	{
		FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* L_39 = V_1;
		NullCheck(L_39);
		int32_t L_40 = L_39->___engineIndex;
		G_B23_0 = L_40;
	}

IL_011b:
	{
		V_4 = G_B23_0;
		RuntimeObject* L_41;
		L_41 = AscentSim_get_Sink_m34F4D7A8103A12F8CECC0E1F780FA136EC04C37A_inline(__this, NULL);
		String_t* L_42 = V_2;
		String_t* L_43 = V_3;
		int32_t L_44 = V_4;
		FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 L_45;
		L_45 = AscentSim_Pos_m9934C7D4D6516D15CED891E1F74635B653FF2E8C(__this, NULL);
		FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 L_46;
		L_46 = AscentSim_Vel_m35E2A7777E74DDE08E59164309DA92B57F7E5508(__this, NULL);
		double L_47;
		L_47 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		NullCheck(L_41);
		InterfaceActionInvoker6< String_t*, String_t*, int32_t, FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7, FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7, double >::Invoke(8, IFlightSimSink_t6B2E44FEAA2B83A736D6ADA46993E42135AF5A95_il2cpp_TypeInfo_var, L_41, L_42, L_43, L_44, L_45, L_46, L_47);
		String_t* L_48 = V_2;
		bool L_49;
		L_49 = FlightEngineMode_IsEngine_m2F9ADE1AC9768AEBCABC4911DDD4D48C6D2FC6CF(L_48, NULL);
		if (!L_49)
		{
			goto IL_0159;
		}
	}
	{
		RuntimeObject* L_50;
		L_50 = AscentSim_get_Sink_m34F4D7A8103A12F8CECC0E1F780FA136EC04C37A_inline(__this, NULL);
		int32_t L_51 = V_4;
		String_t* L_52 = V_2;
		String_t* L_53;
		L_53 = FlightEngineMode_Of_m83A706E318D08DE99B8A9EC5974EDDA0CEB6D861(L_52, NULL);
		NullCheck(L_50);
		InterfaceActionInvoker2< int32_t, String_t* >::Invoke(9, IFlightSimSink_t6B2E44FEAA2B83A736D6ADA46993E42135AF5A95_il2cpp_TypeInfo_var, L_50, L_51, L_53);
	}

IL_0159:
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void AscentSim_EnsureIgnition_mD7BE0F38DF384B744ACA8D0BF38AE349D6149F01 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IFlightSimSink_t6B2E44FEAA2B83A736D6ADA46993E42135AF5A95_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	RuntimeObject* G_B4_0 = NULL;
	RuntimeObject* G_B3_0 = NULL;
	{
		bool L_0 = __this->___ignitionHooked;
		if (!L_0)
		{
			goto IL_0009;
		}
	}
	{
		return;
	}

IL_0009:
	{
		__this->___ignitionHooked = (bool)1;
		RuntimeObject* L_1;
		L_1 = AscentSim_get_Sink_m34F4D7A8103A12F8CECC0E1F780FA136EC04C37A_inline(__this, NULL);
		RuntimeObject* L_2 = L_1;
		if (L_2)
		{
			G_B4_0 = L_2;
			goto IL_001b;
		}
		G_B3_0 = L_2;
	}
	{
		return;
	}

IL_001b:
	{
		NullCheck(G_B4_0);
		InterfaceActionInvoker2< int32_t, double >::Invoke(0, IFlightSimSink_t6B2E44FEAA2B83A736D6ADA46993E42135AF5A95_il2cpp_TypeInfo_var, G_B4_0, 1, (0.0));
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 AscentSim_Pos_m9934C7D4D6516D15CED891E1F74635B653FF2E8C (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_0 = __this->___Y;
		NullCheck(L_0);
		int32_t L_1 = 0;
		double L_2 = (L_0)->GetAt(static_cast<il2cpp_array_size_t>(L_1));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_3 = __this->___Y;
		NullCheck(L_3);
		int32_t L_4 = 1;
		double L_5 = (L_3)->GetAt(static_cast<il2cpp_array_size_t>(L_4));
		FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 L_6;
		memset((&L_6), 0, sizeof(L_6));
		FlightVec__ctor_mCE371A7462C9E8DD33BDB342A3CDF34D1750CFA4((&L_6), L_2, L_5, NULL);
		return L_6;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 AscentSim_Vel_m35E2A7777E74DDE08E59164309DA92B57F7E5508 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_0 = __this->___Y;
		NullCheck(L_0);
		int32_t L_1 = 2;
		double L_2 = (L_0)->GetAt(static_cast<il2cpp_array_size_t>(L_1));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_3 = __this->___Y;
		NullCheck(L_3);
		int32_t L_4 = 3;
		double L_5 = (L_3)->GetAt(static_cast<il2cpp_array_size_t>(L_4));
		FlightVec_t10A40365DF36021CD9DF120F1505ABB1DEADE8D7 L_6;
		memset((&L_6), 0, sizeof(L_6));
		FlightVec__ctor_mCE371A7462C9E8DD33BDB342A3CDF34D1750CFA4((&L_6), L_2, L_5, NULL);
		return L_6;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool AscentSim_HoldingMaxQPitch_m6A1276F1456AD0B8E5EA145533B867A7449BD87B (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, double ___0_alt, double ___1_mass, double ___2_vr, double ___3_vrel, const RuntimeMethod* method) 
{
	double V_0 = 0.0;
	double V_1 = 0.0;
	{
		int32_t L_0;
		L_0 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		if (L_0)
		{
			goto IL_0015;
		}
	}
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_1 = __this->___o;
		NullCheck(L_1);
		GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307* L_2 = L_1->___guidance;
		if (L_2)
		{
			goto IL_0017;
		}
	}

IL_0015:
	{
		return (bool)0;
	}

IL_0017:
	{
		double L_3 = ___0_alt;
		double L_4 = ___3_vrel;
		double L_5;
		L_5 = AscentSim_Dynamic_m84A8DC43B7B0F22A9CBD7991BB4F2D56A8A6693B(__this, L_3, L_4, NULL);
		V_0 = L_5;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_6 = __this->___o;
		NullCheck(L_6);
		GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307* L_7 = L_6->___guidance;
		double L_8 = V_0;
		NullCheck(L_7);
		bool L_9;
		L_9 = GuidanceProfile_HoldKickThroughMaxQ_m23A7CC559FC68E146F24CE31276FB6981B1DD06E(L_7, L_8, NULL);
		if (L_9)
		{
			goto IL_0036;
		}
	}
	{
		return (bool)0;
	}

IL_0036:
	{
		double L_10 = ___0_alt;
		V_1 = ((double)il2cpp_codegen_add(L_10, (6378137.0)));
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_11 = __this->___o;
		NullCheck(L_11);
		GuidanceProfile_t773BD6FFF1F5488A21D3650B7BA762C66948B307* L_12 = L_11->___guidance;
		double L_13 = ___0_alt;
		double L_14 = ___1_mass;
		double L_15 = ___2_vr;
		double L_16 = V_1;
		double L_17 = V_1;
		double L_18 = ___3_vrel;
		GuidanceState_tA40A6AE2010623D64FC40402D0D31113286A0401 L_19;
		L_19 = AscentSim_Guide_mD6EC3412C6E84F3BB7683B700B76EE6FBA7235E1(__this, L_13, L_14, L_15, ((double)((398600441800000.0)/((double)il2cpp_codegen_multiply(L_16, L_17)))), L_18, (0.0), NULL);
		NullCheck(L_12);
		ValueTuple_2_t6535F6090E6D3C9A7A645F4BFD8ED400CBA8B14C L_20;
		L_20 = GuidanceProfile_Command_m0569E0B457499A166FC5720399B70D793882CD00(L_12, L_19, NULL);
		double L_21 = L_20.___Item2;
		return (bool)((((double)L_21) < ((double)(1.0)))? 1 : 0);
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double AscentSim_Dynamic_m84A8DC43B7B0F22A9CBD7991BB4F2D56A8A6693B (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, double ___0_alt, double ___1_vrel, const RuntimeMethod* method) 
{
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_0 = __this->___o;
		NullCheck(L_0);
		bool L_1 = L_0->___vacuum;
		if (L_1)
		{
			goto IL_0025;
		}
	}
	{
		double L_2 = ___0_alt;
		if ((!(((double)L_2) < ((double)(300000.0)))))
		{
			goto IL_0025;
		}
	}
	{
		double L_3 = ___1_vrel;
		if ((((double)L_3) > ((double)(0.0))))
		{
			goto IL_002f;
		}
	}

IL_0025:
	{
		return (0.0);
	}

IL_002f:
	{
		double L_4 = ___0_alt;
		AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 L_5;
		L_5 = Atmosphere_At_mA6E038B8FA424E44AD2A97805AC95A264333ECF7(L_4, NULL);
		double L_6 = L_5.___rho;
		double L_7 = ___1_vrel;
		double L_8 = ___1_vrel;
		return ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((0.5), L_6)), L_7)), L_8));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR GuidanceState_tA40A6AE2010623D64FC40402D0D31113286A0401 AscentSim_Guide_mD6EC3412C6E84F3BB7683B700B76EE6FBA7235E1 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, double ___0_alt, double ___1_m, double ___2_vr, double ___3_g, double ___4_vrel, double ___5_flight, const RuntimeMethod* method) 
{
	double V_0 = 0.0;
	double V_1 = 0.0;
	double V_2 = 0.0;
	double V_3 = 0.0;
	SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* V_4 = NULL;
	GuidanceState_tA40A6AE2010623D64FC40402D0D31113286A0401 V_5;
	memset((&V_5), 0, sizeof(V_5));
	{
		V_0 = (0.0);
		V_1 = (0.0);
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_0 = __this->___o;
		NullCheck(L_0);
		bool L_1 = L_0->___vacuum;
		if (L_1)
		{
			goto IL_0040;
		}
	}
	{
		double L_2 = ___0_alt;
		if ((!(((double)L_2) < ((double)(300000.0)))))
		{
			goto IL_0040;
		}
	}
	{
		double L_3 = ___0_alt;
		AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 L_4;
		L_4 = Atmosphere_At_mA6E038B8FA424E44AD2A97805AC95A264333ECF7(L_3, NULL);
		AtmState_tD8E6EACC86A9F6F36CF77A04A3F941566F994EB1 L_5 = L_4;
		double L_6 = L_5.___p;
		V_0 = L_6;
		double L_7 = L_5.___rho;
		V_1 = L_7;
	}

IL_0040:
	{
		V_2 = (0.0);
		V_3 = (0.0);
		int32_t L_8;
		L_8 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_9 = __this->___st;
		NullCheck(L_9);
		if ((((int32_t)L_8) >= ((int32_t)((int32_t)(((RuntimeArray*)L_9)->max_length)))))
		{
			goto IL_00a2;
		}
	}
	{
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_10 = __this->___st;
		int32_t L_11;
		L_11 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		NullCheck(L_10);
		int32_t L_12 = L_11;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_13 = (L_10)->GetAt(static_cast<il2cpp_array_size_t>(L_12));
		V_4 = L_13;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_14 = V_4;
		NullCheck(L_14);
		double L_15 = L_14->___mdot;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_16 = V_4;
		NullCheck(L_16);
		double L_17 = L_16->___ispSL;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_18 = V_4;
		NullCheck(L_18);
		double L_19 = L_18->___ispVac;
		double L_20 = V_0;
		double L_21;
		L_21 = RocketEq_IspAt_m4FFE489F4D4A8AD0D4199B175E2B729E0727C9B6(L_17, L_19, L_20, NULL);
		V_2 = ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_15, (9.8066499999999994))), L_21));
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_22 = V_4;
		NullCheck(L_22);
		double L_23 = L_22->___throttleMin;
		V_3 = L_23;
	}

IL_00a2:
	{
		il2cpp_codegen_initobj((&V_5), sizeof(GuidanceState_tA40A6AE2010623D64FC40402D0D31113286A0401));
		double L_24 = ___0_alt;
		(&V_5)->___alt = L_24;
		double L_25 = ___4_vrel;
		(&V_5)->___vrel = L_25;
		double L_26 = ___5_flight;
		(&V_5)->___flightPathRad = L_26;
		double L_27 = ___2_vr;
		(&V_5)->___vr = L_27;
		double L_28 = ___3_g;
		(&V_5)->___g = L_28;
		double L_29 = V_1;
		double L_30 = ___4_vrel;
		double L_31 = ___4_vrel;
		(&V_5)->___q = ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((0.5), L_29)), L_30)), L_31));
		double L_32 = ___1_m;
		(&V_5)->___mass = L_32;
		double L_33 = V_2;
		(&V_5)->___thrustN = L_33;
		double L_34 = V_3;
		(&V_5)->___throttleMin = L_34;
		int32_t L_35;
		L_35 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		(&V_5)->___stageIndex = L_35;
		GuidanceState_tA40A6AE2010623D64FC40402D0D31113286A0401 L_36 = V_5;
		return L_36;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool AscentSim_Watch_m57EC47ACCA3B4FA1E4ECA4F5865625DFA39EA697 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, double ___0_alt, double ___1_q, double ___2_m, double ___3_vrel, double ___4_p, double ___5_rho, double ___6_sound, double ___7_avx, double ___8_avy, double ___9_ux, double ___10_uy, double ___11_tx, double ___12_ty, double ___13_dt, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* V_1 = NULL;
	double V_2 = 0.0;
	double V_3 = 0.0;
	double V_4 = 0.0;
	Step_t7AA160BDF44184A07EA62B87484E56D55AC7C795 V_5;
	memset((&V_5), 0, sizeof(V_5));
	SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* V_6 = NULL;
	double V_7 = 0.0;
	double V_8 = 0.0;
	double V_9 = 0.0;
	double V_10 = 0.0;
	double V_11 = 0.0;
	double V_12 = 0.0;
	Step_t7AA160BDF44184A07EA62B87484E56D55AC7C795 V_13;
	memset((&V_13), 0, sizeof(V_13));
	int32_t G_B3_0 = 0;
	double G_B20_0 = 0.0;
	{
		int32_t L_0;
		L_0 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_1 = __this->___st;
		NullCheck(L_1);
		if ((((int32_t)L_0) < ((int32_t)((int32_t)(((RuntimeArray*)L_1)->max_length)))))
		{
			goto IL_001c;
		}
	}
	{
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_2 = __this->___st;
		NullCheck(L_2);
		G_B3_0 = ((int32_t)il2cpp_codegen_subtract(((int32_t)(((RuntimeArray*)L_2)->max_length)), 1));
		goto IL_0022;
	}

IL_001c:
	{
		int32_t L_3;
		L_3 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		G_B3_0 = L_3;
	}

IL_0022:
	{
		V_0 = G_B3_0;
		int32_t L_4 = V_0;
		if ((((int32_t)L_4) >= ((int32_t)0)))
		{
			goto IL_0029;
		}
	}
	{
		V_0 = 0;
	}

IL_0029:
	{
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_5 = __this->___st;
		int32_t L_6 = V_0;
		NullCheck(L_5);
		int32_t L_7 = L_6;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_8 = (L_5)->GetAt(static_cast<il2cpp_array_size_t>(L_7));
		V_1 = L_8;
		V_2 = (1.0);
		bool L_9;
		L_9 = AscentSim_get_Thrusting_m1221CE066B159B7647B0988CCA625853DB547A22_inline(__this, NULL);
		if (!L_9)
		{
			goto IL_00c2;
		}
	}
	{
		int32_t L_10;
		L_10 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_11 = __this->___st;
		NullCheck(L_11);
		if ((((int32_t)L_10) >= ((int32_t)((int32_t)(((RuntimeArray*)L_11)->max_length)))))
		{
			goto IL_00c2;
		}
	}
	{
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_12 = __this->___st;
		int32_t L_13;
		L_13 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		NullCheck(L_12);
		int32_t L_14 = L_13;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_15 = (L_12)->GetAt(static_cast<il2cpp_array_size_t>(L_14));
		NullCheck(L_15);
		double L_16 = L_15->___propMass;
		if ((!(((double)L_16) > ((double)(0.0)))))
		{
			goto IL_00c2;
		}
	}
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_17 = __this->___Y;
		NullCheck(L_17);
		int32_t L_18 = 4;
		double L_19 = (L_17)->GetAt(static_cast<il2cpp_array_size_t>(L_18));
		double L_20;
		L_20 = AscentSim_StageEndMass_mB2D815CE13FEE7A63EAA69082D706CB616D6C623(__this, NULL);
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_21 = __this->___st;
		int32_t L_22;
		L_22 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		NullCheck(L_21);
		int32_t L_23 = L_22;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_24 = (L_21)->GetAt(static_cast<il2cpp_array_size_t>(L_23));
		NullCheck(L_24);
		double L_25 = L_24->___propMass;
		V_2 = ((double)(((double)il2cpp_codegen_subtract(L_19, L_20))/L_25));
		double L_26 = V_2;
		if ((!(((double)L_26) < ((double)(0.0)))))
		{
			goto IL_00ac;
		}
	}
	{
		V_2 = (0.0);
		goto IL_00c2;
	}

IL_00ac:
	{
		double L_27 = V_2;
		if ((!(((double)L_27) > ((double)(1.0)))))
		{
			goto IL_00c2;
		}
	}
	{
		V_2 = (1.0);
	}

IL_00c2:
	{
		V_3 = (0.0);
		bool L_28;
		L_28 = AscentSim_get_Thrusting_m1221CE066B159B7647B0988CCA625853DB547A22_inline(__this, NULL);
		if (!L_28)
		{
			goto IL_019d;
		}
	}
	{
		int32_t L_29;
		L_29 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_30 = __this->___st;
		NullCheck(L_30);
		if ((((int32_t)L_29) >= ((int32_t)((int32_t)(((RuntimeArray*)L_30)->max_length)))))
		{
			goto IL_019d;
		}
	}
	{
		double L_31 = ___2_m;
		if ((!(((double)L_31) > ((double)(0.0)))))
		{
			goto IL_019d;
		}
	}
	{
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_32 = __this->___st;
		int32_t L_33;
		L_33 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		NullCheck(L_32);
		int32_t L_34 = L_33;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_35 = (L_32)->GetAt(static_cast<il2cpp_array_size_t>(L_34));
		V_6 = L_35;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_36 = V_6;
		NullCheck(L_36);
		double L_37 = L_36->___mdot;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_38 = V_6;
		NullCheck(L_38);
		double L_39 = L_38->___ispSL;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_40 = V_6;
		NullCheck(L_40);
		double L_41 = L_40->___ispVac;
		double L_42 = ___4_p;
		double L_43;
		L_43 = RocketEq_IspAt_m4FFE489F4D4A8AD0D4199B175E2B729E0727C9B6(L_39, L_41, L_42, NULL);
		V_7 = ((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(L_37, (9.8066499999999994))), L_43));
		double L_44 = __this->___thrustScale;
		if ((((double)L_44) == ((double)(1.0))))
		{
			goto IL_014d;
		}
	}
	{
		double L_45 = V_7;
		double L_46 = __this->___thrustScale;
		V_7 = ((double)il2cpp_codegen_multiply(L_45, L_46));
	}

IL_014d:
	{
		double L_47 = ___5_rho;
		if ((((double)L_47) > ((double)(0.0))))
		{
			goto IL_0165;
		}
	}
	{
		G_B20_0 = (0.0);
		goto IL_0193;
	}

IL_0165:
	{
		double L_48 = ___5_rho;
		double L_49 = ___3_vrel;
		double L_50 = ___3_vrel;
		double L_51 = ___3_vrel;
		double L_52 = ___6_sound;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_53 = V_6;
		NullCheck(L_53);
		double L_54 = L_53->___cd;
		double L_55;
		L_55 = Aero_CdMach_m0F78C3ADE3138A85FBDF51DC71A3040C035EED47(((double)(L_51/L_52)), L_54, NULL);
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_56 = V_6;
		NullCheck(L_56);
		double L_57 = L_56->___area;
		double L_58 = ___2_m;
		G_B20_0 = ((double)(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply(((double)il2cpp_codegen_multiply((0.5), L_48)), L_49)), L_50)), L_55)), L_57))/L_58));
	}

IL_0193:
	{
		V_8 = G_B20_0;
		double L_59 = V_7;
		double L_60 = ___2_m;
		double L_61 = V_8;
		V_3 = ((double)il2cpp_codegen_subtract(((double)(L_59/L_60)), L_61));
	}

IL_019d:
	{
		V_4 = (0.0);
		bool L_62;
		L_62 = AscentSim_get_Thrusting_m1221CE066B159B7647B0988CCA625853DB547A22_inline(__this, NULL);
		if (!L_62)
		{
			goto IL_0246;
		}
	}
	{
		int32_t L_63;
		L_63 = AscentSim_get_Mode_m5522E937FEF440F09E1F28A50A534066FEA2E2C9_inline(__this, NULL);
		if ((((int32_t)L_63) == ((int32_t)2)))
		{
			goto IL_0246;
		}
	}
	{
		double L_64 = ___3_vrel;
		if ((!(((double)L_64) > ((double)(1.0)))))
		{
			goto IL_0246;
		}
	}
	{
		double L_65;
		L_65 = AscentSim_get_PitchCmd_mC76BA7E009D9C12D9F966E247CC7909311A135ED_inline(__this, NULL);
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_66;
		L_66 = cos(L_65);
		double L_67;
		L_67 = AscentSim_get_PitchCmd_mC76BA7E009D9C12D9F966E247CC7909311A135ED_inline(__this, NULL);
		double L_68;
		L_68 = sin(L_67);
		V_9 = L_68;
		double L_69 = L_66;
		double L_70 = ___11_tx;
		double L_71 = V_9;
		double L_72 = ___9_ux;
		V_10 = ((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_69, L_70)), ((double)il2cpp_codegen_multiply(L_71, L_72))));
		double L_73 = ___12_ty;
		double L_74 = V_9;
		double L_75 = ___10_uy;
		V_11 = ((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_69, L_73)), ((double)il2cpp_codegen_multiply(L_74, L_75))));
		double L_76 = V_10;
		double L_77 = ___7_avx;
		double L_78 = V_11;
		double L_79 = ___8_avy;
		double L_80 = ___3_vrel;
		V_12 = ((double)(((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_76, L_77)), ((double)il2cpp_codegen_multiply(L_78, L_79))))/L_80));
		double L_81 = V_12;
		if ((!(((double)L_81) > ((double)(1.0)))))
		{
			goto IL_0225;
		}
	}
	{
		V_12 = (1.0);
		goto IL_023d;
	}

IL_0225:
	{
		double L_82 = V_12;
		if ((!(((double)L_82) < ((double)(-1.0)))))
		{
			goto IL_023d;
		}
	}
	{
		V_12 = (-1.0);
	}

IL_023d:
	{
		double L_83 = V_12;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_84;
		L_84 = acos(L_83);
		V_4 = L_84;
	}

IL_0246:
	{
		il2cpp_codegen_initobj((&V_13), sizeof(Step_t7AA160BDF44184A07EA62B87484E56D55AC7C795));
		double L_85;
		L_85 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		(&V_13)->___t = L_85;
		double L_86 = ___13_dt;
		(&V_13)->___dt = L_86;
		double L_87 = ___0_alt;
		(&V_13)->___alt = L_87;
		double L_88 = ___1_q;
		(&V_13)->___qPa = L_88;
		double L_89 = V_3;
		(&V_13)->___accel = L_89;
		double L_90 = V_4;
		(&V_13)->___alphaRad = L_90;
		double L_91 = ___2_m;
		(&V_13)->___mass = L_91;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_92 = V_1;
		NullCheck(L_92);
		double L_93 = L_92->___area;
		(&V_13)->___area = L_93;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_94 = V_1;
		NullCheck(L_94);
		double L_95 = L_94->___cd;
		(&V_13)->___cd = L_95;
		int32_t L_96;
		L_96 = AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline(__this, NULL);
		(&V_13)->___stageIndex = L_96;
		double L_97 = V_2;
		(&V_13)->___propFraction = L_97;
		bool L_98;
		L_98 = AscentSim_get_Thrusting_m1221CE066B159B7647B0988CCA625853DB547A22_inline(__this, NULL);
		(&V_13)->___thrusting = L_98;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_99 = __this->___o;
		NullCheck(L_99);
		double L_100 = L_99->___targetAlt;
		(&V_13)->___targetAlt = L_100;
		Step_t7AA160BDF44184A07EA62B87484E56D55AC7C795 L_101 = V_13;
		V_5 = L_101;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_102 = __this->___o;
		NullCheck(L_102);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_103 = L_102->___monitor;
		NullCheck(L_103);
		bool L_104;
		L_104 = FailureMonitor_Consider_m33EC84A720E446BEF14613644415890C477192BC(L_103, (&V_5), NULL);
		return L_104;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double AscentSim_CoastApexAlt_mC04DF06DBA61D0C0342EA5D61312CCD9A4D5186D (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral1E3DB61522C4E7AA1D5A216FFA2CA92BAFA2EBF1);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral88CE3FCD887A58F8A5764032FA39B0C1CD48440B);
		s_Il2CppMethodInitialized = true;
	}
	DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* V_0 = NULL;
	double V_1 = 0.0;
	double V_2 = 0.0;
	int32_t V_3 = 0;
	double V_4 = 0.0;
	double V_5 = 0.0;
	double G_B7_0 = 0.0;
	{
		bool L_0;
		L_0 = AscentSim_get_Thrusting_m1221CE066B159B7647B0988CCA625853DB547A22_inline(__this, NULL);
		if (L_0)
		{
			goto IL_002c;
		}
	}
	{
		String_t* L_1 = __this->___outcome;
		bool L_2;
		L_2 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_1, _stringLiteral1E3DB61522C4E7AA1D5A216FFA2CA92BAFA2EBF1, NULL);
		if (L_2)
		{
			goto IL_002c;
		}
	}
	{
		String_t* L_3 = __this->___outcome;
		bool L_4;
		L_4 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_3, _stringLiteral88CE3FCD887A58F8A5764032FA39B0C1CD48440B, NULL);
		if (!L_4)
		{
			goto IL_0036;
		}
	}

IL_002c:
	{
		return (std::numeric_limits<double>::quiet_NaN());
	}

IL_0036:
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_5 = __this->___Y;
		NullCheck((RuntimeArray*)L_5);
		RuntimeObject* L_6;
		L_6 = Array_Clone_m66C9D0727C9BAA0995E4142F29B45BC03582E042((RuntimeArray*)L_5, NULL);
		V_0 = ((DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE*)Castclass((RuntimeObject*)L_6, DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE_il2cpp_TypeInfo_var));
		double L_7;
		L_7 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		V_1 = L_7;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_8 = __this->___o;
		NullCheck(L_8);
		double L_9 = L_8->___dt;
		if ((((double)L_9) > ((double)(0.0))))
		{
			goto IL_006f;
		}
	}
	{
		G_B7_0 = (0.050000000000000003);
		goto IL_007a;
	}

IL_006f:
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_10 = __this->___o;
		NullCheck(L_10);
		double L_11 = L_10->___dt;
		G_B7_0 = L_11;
	}

IL_007a:
	{
		V_2 = G_B7_0;
		V_3 = 0;
		goto IL_0113;
	}

IL_0082:
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_12 = V_0;
		NullCheck(L_12);
		int32_t L_13 = 0;
		double L_14 = (L_12)->GetAt(static_cast<il2cpp_array_size_t>(L_13));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_15 = V_0;
		NullCheck(L_15);
		int32_t L_16 = 1;
		double L_17 = (L_15)->GetAt(static_cast<il2cpp_array_size_t>(L_16));
		double L_18;
		L_18 = JsMath_Hypot_mACE54445FD52A26990FD1AD7C43FDC73B6F893D4(L_14, L_17, NULL);
		V_4 = L_18;
		double L_19 = V_4;
		V_5 = ((double)il2cpp_codegen_subtract(L_19, (6378137.0)));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_20 = V_0;
		NullCheck(L_20);
		int32_t L_21 = 2;
		double L_22 = (L_20)->GetAt(static_cast<il2cpp_array_size_t>(L_21));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_23 = V_0;
		NullCheck(L_23);
		int32_t L_24 = 0;
		double L_25 = (L_23)->GetAt(static_cast<il2cpp_array_size_t>(L_24));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_26 = V_0;
		NullCheck(L_26);
		int32_t L_27 = 3;
		double L_28 = (L_26)->GetAt(static_cast<il2cpp_array_size_t>(L_27));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_29 = V_0;
		NullCheck(L_29);
		int32_t L_30 = 1;
		double L_31 = (L_29)->GetAt(static_cast<il2cpp_array_size_t>(L_30));
		double L_32 = V_4;
		if ((!(((double)((double)(((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply(L_22, L_25)), ((double)il2cpp_codegen_multiply(L_28, L_31))))/L_32))) <= ((double)(0.0)))))
		{
			goto IL_00bd;
		}
	}
	{
		double L_33 = V_5;
		return L_33;
	}

IL_00bd:
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_34 = __this->___o;
		NullCheck(L_34);
		bool L_35 = L_34->___vacuum;
		if (L_35)
		{
			goto IL_00d7;
		}
	}
	{
		double L_36 = V_5;
		if ((!(((double)L_36) >= ((double)(300000.0)))))
		{
			goto IL_00f7;
		}
	}

IL_00d7:
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_37 = V_0;
		NullCheck(L_37);
		int32_t L_38 = 0;
		double L_39 = (L_37)->GetAt(static_cast<il2cpp_array_size_t>(L_38));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_40 = V_0;
		NullCheck(L_40);
		int32_t L_41 = 1;
		double L_42 = (L_40)->GetAt(static_cast<il2cpp_array_size_t>(L_41));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_43 = V_0;
		NullCheck(L_43);
		int32_t L_44 = 2;
		double L_45 = (L_43)->GetAt(static_cast<il2cpp_array_size_t>(L_44));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_46 = V_0;
		NullCheck(L_46);
		int32_t L_47 = 3;
		double L_48 = (L_46)->GetAt(static_cast<il2cpp_array_size_t>(L_47));
		OrbitElements_t5EEC91C4BE0D34654FC26DBE704E101E14BB7D98 L_49;
		L_49 = Orbit_Planar_m81894386C1A7EA3C3C48E177AAAF4946DBF669E7(L_39, L_42, L_45, L_48, (398600441800000.0), NULL);
		double L_50 = L_49.___apoAlt;
		return L_50;
	}

IL_00f7:
	{
		RK4_t1BC6735398DED08AF0675CEEB7983FDA7AB140CB* L_51 = __this->___rk;
		Deriv_t32B789F19EA5F7301FEE48A702C2B32AF64EF69A* L_52 = __this->___deriv;
		double L_53 = V_1;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_54 = V_0;
		double L_55 = V_2;
		NullCheck(L_51);
		RK4_Step_mA370B9F53F0B0CD1473EFDED893537324B37C9A2(L_51, L_52, L_53, L_54, L_55, NULL);
		double L_56 = V_1;
		double L_57 = V_2;
		V_1 = ((double)il2cpp_codegen_add(L_56, L_57));
		int32_t L_58 = V_3;
		V_3 = ((int32_t)il2cpp_codegen_add(L_58, 1));
	}

IL_0113:
	{
		int32_t L_59 = V_3;
		if ((((int32_t)L_59) < ((int32_t)((int32_t)400000))))
		{
			goto IL_0082;
		}
	}
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_60 = V_0;
		NullCheck(L_60);
		int32_t L_61 = 0;
		double L_62 = (L_60)->GetAt(static_cast<il2cpp_array_size_t>(L_61));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_63 = V_0;
		NullCheck(L_63);
		int32_t L_64 = 1;
		double L_65 = (L_63)->GetAt(static_cast<il2cpp_array_size_t>(L_64));
		double L_66;
		L_66 = JsMath_Hypot_mACE54445FD52A26990FD1AD7C43FDC73B6F893D4(L_62, L_65, NULL);
		return ((double)il2cpp_codegen_subtract(L_66, (6378137.0)));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* AscentSim_Finish_m38F11D0801DA64CA9E5EECC1BCB1C1F6A720A6D0 (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IFlightSimSink_t6B2E44FEAA2B83A736D6ADA46993E42135AF5A95_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_Add_mF10DB1D3CBB0B14215F0E4F8AB4934A1955E5351_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1__ctor_mCA8DD57EAC70C2B5923DBB9D5A77CEAC22E7068E_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_tF470A3BE5C1B5B68E1325EF3F109D172E60BD7CD_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral0801BF8830970233C3B134BEF1CE110C18B5CCC7);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral1DD92AA4AFE85FDFA27394CEC51EF50B82AFF2A0);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral4044F71E1BE65840634E2AF1C09536F10DBF2AF5);
		s_Il2CppMethodInitialized = true;
	}
	OrbitElements_t5EEC91C4BE0D34654FC26DBE704E101E14BB7D98 V_0;
	memset((&V_0), 0, sizeof(V_0));
	List_1_tF470A3BE5C1B5B68E1325EF3F109D172E60BD7CD* V_1 = NULL;
	SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* V_2 = NULL;
	SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* V_3 = NULL;
	int32_t V_4 = 0;
	SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* V_5 = NULL;
	Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 V_6;
	memset((&V_6), 0, sizeof(V_6));
	RuntimeObject* G_B4_0 = NULL;
	RuntimeObject* G_B3_0 = NULL;
	AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* G_B9_0 = NULL;
	AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* G_B8_0 = NULL;
	String_t* G_B10_0 = NULL;
	AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* G_B10_1 = NULL;
	SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* G_B20_0 = NULL;
	SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* G_B20_1 = NULL;
	SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* G_B19_0 = NULL;
	SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* G_B19_1 = NULL;
	Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 G_B21_0;
	memset((&G_B21_0), 0, sizeof(G_B21_0));
	SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* G_B21_1 = NULL;
	SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* G_B21_2 = NULL;
	{
		AscentSim_EnsureIgnition_mD7BE0F38DF384B744ACA8D0BF38AE349D6149F01(__this, NULL);
		bool L_0 = __this->___maxQPassed;
		if (L_0)
		{
			goto IL_0043;
		}
	}
	{
		double L_1 = __this->___maxQ;
		if ((!(((double)L_1) > ((double)(0.0)))))
		{
			goto IL_0043;
		}
	}
	{
		__this->___maxQPassed = (bool)1;
		RuntimeObject* L_2;
		L_2 = AscentSim_get_Sink_m34F4D7A8103A12F8CECC0E1F780FA136EC04C37A_inline(__this, NULL);
		RuntimeObject* L_3 = L_2;
		if (L_3)
		{
			G_B4_0 = L_3;
			goto IL_0032;
		}
		G_B3_0 = L_3;
	}
	{
		goto IL_0043;
	}

IL_0032:
	{
		double L_4 = __this->___maxQ;
		double L_5 = __this->___maxQTime;
		NullCheck(G_B4_0);
		InterfaceActionInvoker2< double, double >::Invoke(2, IFlightSimSink_t6B2E44FEAA2B83A736D6ADA46993E42135AF5A95_il2cpp_TypeInfo_var, G_B4_0, L_4, L_5);
	}

IL_0043:
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_6 = __this->___Y;
		NullCheck(L_6);
		int32_t L_7 = 0;
		double L_8 = (L_6)->GetAt(static_cast<il2cpp_array_size_t>(L_7));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_9 = __this->___Y;
		NullCheck(L_9);
		int32_t L_10 = 1;
		double L_11 = (L_9)->GetAt(static_cast<il2cpp_array_size_t>(L_10));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_12 = __this->___Y;
		NullCheck(L_12);
		int32_t L_13 = 2;
		double L_14 = (L_12)->GetAt(static_cast<il2cpp_array_size_t>(L_13));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_15 = __this->___Y;
		NullCheck(L_15);
		int32_t L_16 = 3;
		double L_17 = (L_15)->GetAt(static_cast<il2cpp_array_size_t>(L_16));
		OrbitElements_t5EEC91C4BE0D34654FC26DBE704E101E14BB7D98 L_18;
		L_18 = Orbit_Planar_m81894386C1A7EA3C3C48E177AAAF4946DBF669E7(L_8, L_11, L_14, L_17, (398600441800000.0), NULL);
		V_0 = L_18;
		String_t* L_19 = __this->___outcome;
		bool L_20;
		L_20 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_19, _stringLiteral4044F71E1BE65840634E2AF1C09536F10DBF2AF5, NULL);
		if (!L_20)
		{
			goto IL_00af;
		}
	}
	{
		bool L_21;
		L_21 = AscentSim_get_Thrusting_m1221CE066B159B7647B0988CCA625853DB547A22_inline(__this, NULL);
		if (L_21)
		{
			goto IL_00af;
		}
	}
	{
		OrbitElements_t5EEC91C4BE0D34654FC26DBE704E101E14BB7D98 L_22 = V_0;
		double L_23 = L_22.___periAlt;
		if ((((double)L_23) > ((double)(120000.0))))
		{
			G_B9_0 = __this;
			goto IL_00a5;
		}
		G_B8_0 = __this;
	}
	{
		G_B10_0 = _stringLiteral1DD92AA4AFE85FDFA27394CEC51EF50B82AFF2A0;
		G_B10_1 = G_B8_0;
		goto IL_00aa;
	}

IL_00a5:
	{
		G_B10_0 = _stringLiteral0801BF8830970233C3B134BEF1CE110C18B5CCC7;
		G_B10_1 = G_B9_0;
	}

IL_00aa:
	{
		NullCheck(G_B10_1);
		G_B10_1->___outcome = G_B10_0;
		Il2CppCodeGenWriteBarrier((void**)(&G_B10_1->___outcome), (void*)G_B10_0);
	}

IL_00af:
	{
		String_t* L_24 = __this->___outcome;
		bool L_25;
		L_25 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_24, _stringLiteral0801BF8830970233C3B134BEF1CE110C18B5CCC7, NULL);
		if (!L_25)
		{
			goto IL_010a;
		}
	}
	{
		List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5* L_26 = __this->___Events;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_27 = (SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9*)il2cpp_codegen_object_new(SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9_il2cpp_TypeInfo_var);
		SimEvent__ctor_m9601BFE450112DDA042CB7CB0DA82C273CCA7367(L_27, NULL);
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_28 = L_27;
		double L_29;
		L_29 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		NullCheck(L_28);
		L_28->___t = L_29;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_30 = L_28;
		NullCheck(L_30);
		L_30->___type = _stringLiteral0801BF8830970233C3B134BEF1CE110C18B5CCC7;
		Il2CppCodeGenWriteBarrier((void**)(&L_30->___type), (void*)_stringLiteral0801BF8830970233C3B134BEF1CE110C18B5CCC7);
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_31 = L_30;
		OrbitElements_t5EEC91C4BE0D34654FC26DBE704E101E14BB7D98 L_32 = V_0;
		double L_33 = L_32.___apoAlt;
		Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 L_34;
		memset((&L_34), 0, sizeof(L_34));
		Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF((&L_34), L_33, Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF_RuntimeMethod_var);
		NullCheck(L_31);
		L_31->___apoAlt = L_34;
		SimEvent_t39775C545FD622A6766F454A6C174D2BBE4CA1B9* L_35 = L_31;
		OrbitElements_t5EEC91C4BE0D34654FC26DBE704E101E14BB7D98 L_36 = V_0;
		double L_37 = L_36.___periAlt;
		Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 L_38;
		memset((&L_38), 0, sizeof(L_38));
		Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF((&L_38), L_37, Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF_RuntimeMethod_var);
		NullCheck(L_35);
		L_35->___periAlt = L_38;
		NullCheck(L_26);
		List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_inline(L_26, L_35, List_1_Add_m630E055BF3C5A497C47705A17B1389ED6EC8EA4A_RuntimeMethod_var);
	}

IL_010a:
	{
		List_1_tF470A3BE5C1B5B68E1325EF3F109D172E60BD7CD* L_39 = (List_1_tF470A3BE5C1B5B68E1325EF3F109D172E60BD7CD*)il2cpp_codegen_object_new(List_1_tF470A3BE5C1B5B68E1325EF3F109D172E60BD7CD_il2cpp_TypeInfo_var);
		List_1__ctor_mCA8DD57EAC70C2B5923DBB9D5A77CEAC22E7068E(L_39, List_1__ctor_mCA8DD57EAC70C2B5923DBB9D5A77CEAC22E7068E_RuntimeMethod_var);
		V_1 = L_39;
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_40 = __this->___st;
		V_3 = L_40;
		V_4 = 0;
		goto IL_013e;
	}

IL_011c:
	{
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_41 = V_3;
		int32_t L_42 = V_4;
		NullCheck(L_41);
		int32_t L_43 = L_42;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_44 = (L_41)->GetAt(static_cast<il2cpp_array_size_t>(L_43));
		V_5 = L_44;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_45 = V_5;
		NullCheck(L_45);
		String_t* L_46 = L_45->___warn;
		if (!L_46)
		{
			goto IL_0138;
		}
	}
	{
		List_1_tF470A3BE5C1B5B68E1325EF3F109D172E60BD7CD* L_47 = V_1;
		SimStage_tA53993AF2ACB4907BA58C1B689DA3750AA91266E* L_48 = V_5;
		NullCheck(L_48);
		String_t* L_49 = L_48->___warn;
		NullCheck(L_47);
		List_1_Add_mF10DB1D3CBB0B14215F0E4F8AB4934A1955E5351_inline(L_47, L_49, List_1_Add_mF10DB1D3CBB0B14215F0E4F8AB4934A1955E5351_RuntimeMethod_var);
	}

IL_0138:
	{
		int32_t L_50 = V_4;
		V_4 = ((int32_t)il2cpp_codegen_add(L_50, 1));
	}

IL_013e:
	{
		int32_t L_51 = V_4;
		SimStageU5BU5D_tFB684CEC42DB0819AE88F4A256F3B2BB6DD8E641* L_52 = V_3;
		NullCheck(L_52);
		if ((((int32_t)L_51) < ((int32_t)((int32_t)(((RuntimeArray*)L_52)->max_length)))))
		{
			goto IL_011c;
		}
	}
	{
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_53 = (SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD*)il2cpp_codegen_object_new(SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD_il2cpp_TypeInfo_var);
		SimResult__ctor_m6EFFEB4A55446AFAE578555518F726943D7DFD4D(L_53, NULL);
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_54 = L_53;
		String_t* L_55 = __this->___outcome;
		NullCheck(L_54);
		L_54->___outcome = L_55;
		Il2CppCodeGenWriteBarrier((void**)(&L_54->___outcome), (void*)L_55);
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_56 = L_54;
		double L_57;
		L_57 = AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline(__this, NULL);
		NullCheck(L_56);
		L_56->___time = L_57;
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_58 = L_56;
		List_1_t17ACB0A1361AE95D1C268DC73CE1D548052596C5* L_59 = __this->___Events;
		NullCheck(L_58);
		L_58->___events = L_59;
		Il2CppCodeGenWriteBarrier((void**)(&L_58->___events), (void*)L_59);
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_60 = L_58;
		OrbitElements_t5EEC91C4BE0D34654FC26DBE704E101E14BB7D98 L_61 = V_0;
		NullCheck(L_60);
		L_60->___orbit = L_61;
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_62 = L_60;
		double L_63 = __this->___maxQ;
		NullCheck(L_62);
		L_62->___maxQ = L_63;
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_64 = L_62;
		double L_65 = __this->___maxQTime;
		NullCheck(L_64);
		L_64->___maxQTime = L_65;
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_66 = L_64;
		double L_67 = __this->___maxQAlt;
		NullCheck(L_66);
		L_66->___maxQAlt = L_67;
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_68 = L_66;
		double L_69 = __this->___maxG;
		NullCheck(L_68);
		L_68->___maxG = L_69;
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_70 = L_68;
		double L_71 = __this->___lossG;
		NullCheck(L_70);
		L_70->___lossGravity = L_71;
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_72 = L_70;
		double L_73 = __this->___lossD;
		NullCheck(L_72);
		L_72->___lossDrag = L_73;
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_74 = L_72;
		double L_75 = __this->___lossS;
		NullCheck(L_74);
		L_74->___lossSteering = L_75;
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_76 = L_74;
		RuntimeObject* L_77 = __this->___stagesIn;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_78 = __this->___o;
		NullCheck(L_78);
		double L_79 = L_78->___payload;
		ValueTuple_2_tCD83B33C76656F32DCB440F5228F21BF25762DE2 L_80;
		L_80 = Headless_StackDeltaV_m9C51D98CCE8BDDE9B7F7F6014E1947219FC73C3C(L_77, L_79, NULL);
		double L_81 = L_80.___Item2;
		NullCheck(L_76);
		L_76->___idealDeltaV = L_81;
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_82 = L_76;
		double L_83 = __this->___dvExp;
		NullCheck(L_82);
		L_82->___deltaVExpended = L_83;
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_84 = L_82;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_85 = __this->___Y;
		NullCheck(L_85);
		int32_t L_86 = 4;
		double L_87 = (L_85)->GetAt(static_cast<il2cpp_array_size_t>(L_86));
		NullCheck(L_84);
		L_84->___finalMass = L_87;
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_88 = L_84;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_89 = __this->___Y;
		NullCheck(L_89);
		int32_t L_90 = 0;
		double L_91 = (L_89)->GetAt(static_cast<il2cpp_array_size_t>(L_90));
		NullCheck(L_88);
		L_88->___x = L_91;
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_92 = L_88;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_93 = __this->___Y;
		NullCheck(L_93);
		int32_t L_94 = 1;
		double L_95 = (L_93)->GetAt(static_cast<il2cpp_array_size_t>(L_94));
		NullCheck(L_92);
		L_92->___y = L_95;
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_96 = L_92;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_97 = __this->___Y;
		NullCheck(L_97);
		int32_t L_98 = 2;
		double L_99 = (L_97)->GetAt(static_cast<il2cpp_array_size_t>(L_98));
		NullCheck(L_96);
		L_96->___vx = L_99;
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_100 = L_96;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_101 = __this->___Y;
		NullCheck(L_101);
		int32_t L_102 = 3;
		double L_103 = (L_101)->GetAt(static_cast<il2cpp_array_size_t>(L_102));
		NullCheck(L_100);
		L_100->___vy = L_103;
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_104 = L_100;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_105 = __this->___Y;
		NullCheck(L_105);
		int32_t L_106 = 4;
		double L_107 = (L_105)->GetAt(static_cast<il2cpp_array_size_t>(L_106));
		NullCheck(L_104);
		L_104->___m = L_107;
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_108 = L_104;
		List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8* L_109 = __this->___Samples;
		NullCheck(L_108);
		L_108->___samples = L_109;
		Il2CppCodeGenWriteBarrier((void**)(&L_108->___samples), (void*)L_109);
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_110 = L_108;
		List_1_tF470A3BE5C1B5B68E1325EF3F109D172E60BD7CD* L_111 = V_1;
		NullCheck(L_110);
		L_110->___warnings = L_111;
		Il2CppCodeGenWriteBarrier((void**)(&L_110->___warnings), (void*)L_111);
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_112 = L_110;
		bool L_113 = __this->___sawKarman;
		if (L_113)
		{
			G_B20_0 = L_112;
			G_B20_1 = L_112;
			goto IL_0272;
		}
		G_B19_0 = L_112;
		G_B19_1 = L_112;
	}
	{
		il2cpp_codegen_initobj((&V_6), sizeof(Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165));
		Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 L_114 = V_6;
		G_B21_0 = L_114;
		G_B21_1 = G_B19_0;
		G_B21_2 = G_B19_1;
		goto IL_027d;
	}

IL_0272:
	{
		double L_115 = __this->___karmanT;
		Nullable_1_t6E154519A812D040E3016229CD7638843A2CC165 L_116;
		memset((&L_116), 0, sizeof(L_116));
		Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF((&L_116), L_115, Nullable_1__ctor_mDE3158DD99CA7E1775A8BA276E428AF808AB8FBF_RuntimeMethod_var);
		G_B21_0 = L_116;
		G_B21_1 = G_B20_0;
		G_B21_2 = G_B20_1;
	}

IL_027d:
	{
		NullCheck(G_B21_1);
		G_B21_1->___KarmanTime = G_B21_0;
		V_2 = G_B21_2;
		OrbitElements_t5EEC91C4BE0D34654FC26DBE704E101E14BB7D98 L_117 = V_0;
		double L_118 = L_117.___periAlt;
		if ((!(((double)L_118) < ((double)(0.0)))))
		{
			goto IL_02a0;
		}
	}
	{
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_119 = V_2;
		double L_120;
		L_120 = AscentSim_CoastApexAlt_mC04DF06DBA61D0C0342EA5D61312CCD9A4D5186D(__this, NULL);
		NullCheck(L_119);
		L_119->___coastApexAlt = L_120;
	}

IL_02a0:
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_121 = __this->___o;
		NullCheck(L_121);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_122 = L_121->___monitor;
		if (!L_122)
		{
			goto IL_0334;
		}
	}
	{
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_123 = __this->___o;
		NullCheck(L_123);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_124 = L_123->___monitor;
		double L_125 = __this->___maxQ;
		NullCheck(L_124);
		FailureMonitor_Finish_mF516837311D82349AEF7B690911B8DB45DE61B6C(L_124, L_125, NULL);
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_126 = V_2;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_127 = __this->___o;
		NullCheck(L_127);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_128 = L_127->___monitor;
		NullCheck(L_128);
		FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* L_129 = L_128->___Event;
		NullCheck(L_126);
		L_126->___failure = L_129;
		Il2CppCodeGenWriteBarrier((void**)(&L_126->___failure), (void*)L_129);
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_130 = V_2;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_131 = __this->___o;
		NullCheck(L_131);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_132 = L_131->___monitor;
		NullCheck(L_132);
		List_1_tAB32C91F17A9EF1C977FD736177219D0D9DD679A* L_133 = L_132->___Risks;
		NullCheck(L_130);
		L_130->___risks = L_133;
		Il2CppCodeGenWriteBarrier((void**)(&L_130->___risks), (void*)L_133);
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_134 = V_2;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_135 = __this->___o;
		NullCheck(L_135);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_136 = L_135->___monitor;
		NullCheck(L_136);
		double L_137;
		L_137 = FailureMonitor_get_MinStability_m9BDC7DF918EBBC3F7CE69DA67F1BD3E4DF72C8A3(L_136, NULL);
		NullCheck(L_134);
		L_134->___minStability = L_137;
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_138 = V_2;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_139 = __this->___o;
		NullCheck(L_139);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_140 = L_139->___monitor;
		NullCheck(L_140);
		double L_141;
		L_141 = FailureMonitor_get_MinCar_m99D4B873322CA59A57C9EE12A123519B6B408BEA(L_140, NULL);
		NullCheck(L_138);
		L_138->___minCar = L_141;
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_142 = V_2;
		SimOptions_t481383310E68D44D0E408386AC2B4B3D8026D51B* L_143 = __this->___o;
		NullCheck(L_143);
		FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* L_144 = L_143->___monitor;
		NullCheck(L_144);
		double L_145;
		L_145 = FailureMonitor_get_QLimPa_mB3D80483EFA73C221443397E6D960B724E0353FE_inline(L_144, NULL);
		NullCheck(L_142);
		L_142->___qLimPa = L_145;
	}

IL_0334:
	{
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_146 = V_2;
		NullCheck(L_146);
		FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* L_147 = L_146->___failure;
		if (L_147)
		{
			goto IL_0350;
		}
	}
	{
		FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* L_148 = __this->___playedBreak;
		if (!L_148)
		{
			goto IL_0350;
		}
	}
	{
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_149 = V_2;
		FailureEvent_tA298E4B8EA78872D8F7B343C8B3FB0F380BDED57* L_150 = __this->___playedBreak;
		NullCheck(L_149);
		L_149->___failure = L_150;
		Il2CppCodeGenWriteBarrier((void**)(&L_149->___failure), (void*)L_150);
	}

IL_0350:
	{
		SimResult_t52B6CE241C484A6010F7D2C434DE7E0EF28332BD* L_151 = V_2;
		return L_151;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void AscentSim_set_Sink_m83A341CB52EE9FD70B2BF88750BE6F611CC75295_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, RuntimeObject* ___0_value, const RuntimeMethod* method) 
{
	{
		RuntimeObject* L_0 = ___0_value;
		__this->___U3CSinkU3Ek__BackingField = L_0;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___U3CSinkU3Ek__BackingField), (void*)L_0);
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool AscentSim_get_Thrusting_m1221CE066B159B7647B0988CCA625853DB547A22_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		bool L_0 = __this->___U3CThrustingU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t AscentSim_get_StageIdx_m536AFCCCE220FA102EA5709693E591862A6029CB_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___U3CStageIdxU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool Double_IsNaN_mF2BC6D1FD4813179B2CAE58D29770E42830D0883_inline (double ___0_d, const RuntimeMethod* method) 
{
	{
		double L_0 = ___0_d;
		int64_t L_1;
		L_1 = BitConverter_DoubleToInt64Bits_m4F42741818550F9956B5FBAF88C051F4DE5B0AE6_inline(L_0, NULL);
		return (bool)((((int64_t)((int64_t)(L_1&((int64_t)(std::numeric_limits<int64_t>::max)())))) > ((int64_t)((int64_t)9218868437227405312LL)))? 1 : 0);
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t AscentSim_get_Mode_m5522E937FEF440F09E1F28A50A534066FEA2E2C9_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___U3CModeU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR double AscentSim_get_PitchCmd_mC76BA7E009D9C12D9F966E247CC7909311A135ED_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		double L_0 = __this->___U3CPitchCmdU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool AscentSim_get_Done_mBC8EEA714EDD0073C9F4D91135D515D022B41A6A_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		bool L_0 = __this->___U3CDoneU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR double AscentSim_get_T_m519D786925BDF8D9FB777AD08DA8AF9DC34F7C03_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		double L_0 = __this->___U3CTU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void AscentSim_set_Done_m95D7993A7D005264297DDDDF34649C09515E702C_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, bool ___0_value, const RuntimeMethod* method) 
{
	{
		bool L_0 = ___0_value;
		__this->___U3CDoneU3Ek__BackingField = L_0;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void AscentSim_set_Mode_m7946FC27FD607C87B37DB52807785705728CFDC3_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, int32_t ___0_value, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = ___0_value;
		__this->___U3CModeU3Ek__BackingField = L_0;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void AscentSim_set_PitchCmd_m54C6E65BF6705E8F4DD4DC357074BD0DF8CA677A_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, double ___0_value, const RuntimeMethod* method) 
{
	{
		double L_0 = ___0_value;
		__this->___U3CPitchCmdU3Ek__BackingField = L_0;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void AscentSim_set_StageIdx_m0D20B990A04C0A885ABBD3A3B4861165D5091CAF_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, int32_t ___0_value, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = ___0_value;
		__this->___U3CStageIdxU3Ek__BackingField = L_0;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void AscentSim_set_Thrusting_mD9D054E78DF826C6D731FFE51269A92EBD1D3992_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, bool ___0_value, const RuntimeMethod* method) 
{
	{
		bool L_0 = ___0_value;
		__this->___U3CThrustingU3Ek__BackingField = L_0;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR RuntimeObject* AscentSim_get_Sink_m34F4D7A8103A12F8CECC0E1F780FA136EC04C37A_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, const RuntimeMethod* method) 
{
	{
		RuntimeObject* L_0 = __this->___U3CSinkU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void AscentSim_set_T_mECCDAC4D95A8329E97710AAC1A222166080AE6F5_inline (AscentSim_t046EDEBA3D408F3143956A89639272683FDD441C* __this, double ___0_value, const RuntimeMethod* method) 
{
	{
		double L_0 = ___0_value;
		__this->___U3CTU3Ek__BackingField = L_0;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR double FailureMonitor_get_QLimPa_mB3D80483EFA73C221443397E6D960B724E0353FE_inline (FailureMonitor_t3A9499C2E8527D908085BE405581EF754243C146* __this, const RuntimeMethod* method) 
{
	{
		double L_0 = __this->___qLimPa;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Span_1__ctor_mBCBB8DE851A47E109FC93829B35583C922A5EB17_gshared_inline (Span_1_t49889ACD274FAD18EF7DE6B84826A3AACAEE32B5* __this, void* ___0_pointer, int32_t ___1_length, const RuntimeMethod* method) 
{
	{
		goto IL_0016;
	}

IL_0016:
	{
		int32_t L_0 = ___1_length;
		if ((((int32_t)L_0) >= ((int32_t)0)))
		{
			goto IL_001f;
		}
	}
	{
		ThrowHelper_ThrowArgumentOutOfRangeException_mD7D90276EDCDF9394A8EA635923E3B48BB71BD56(NULL);
	}

IL_001f:
	{
		void* L_1 = ___0_pointer;
		double* L_2;
		L_2 = il2cpp_unsafe_as_ref<double>((uint8_t*)L_1);
		ByReference_1_t4505442DB99F393450BB394E5A719E096875B1AC L_3;
		memset((&L_3), 0, sizeof(L_3));
		il2cpp_codegen_by_reference_constructor((Il2CppByReference*)(&L_3), L_2);
		__this->____pointer = L_3;
		int32_t L_4 = ___1_length;
		__this->____length = L_4;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool Nullable_1_get_HasValue_mB1F55188CDD50D6D725D41F55D2F2540CD15FB20_gshared_inline (Nullable_1_t163D49A1147F217B7BD43BE8ACC8A5CC6B846D14* __this, const RuntimeMethod* method) 
{
	{
		bool L_0 = __this->___hasValue;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Nullable_1_GetValueOrDefault_m26287DBB36429B2E2FBCB3F96F439248F8CAA8BB_gshared_inline (Nullable_1_t163D49A1147F217B7BD43BE8ACC8A5CC6B846D14* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___value;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool Nullable_1_get_HasValue_mCF2FD8B3055FA87FC9C504F2122B3B0FAEDE3EC9_gshared_inline (Nullable_1_tCF32C56A2641879C053C86F273C0C6EC1B40BC28* __this, const RuntimeMethod* method) 
{
	{
		bool L_0 = __this->___hasValue;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void List_1_Add_mEBCF994CC3814631017F46A387B1A192ED6C85C7_gshared_inline (List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D* __this, RuntimeObject* ___0_item, const RuntimeMethod* method) 
{
	ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918* V_0 = NULL;
	int32_t V_1 = 0;
	{
		int32_t L_0 = __this->____version;
		__this->____version = ((int32_t)il2cpp_codegen_add(L_0, 1));
		ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918* L_1 = __this->____items;
		V_0 = L_1;
		int32_t L_2 = __this->____size;
		V_1 = L_2;
		int32_t L_3 = V_1;
		ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918* L_4 = V_0;
		NullCheck(L_4);
		if ((!(((uint32_t)L_3) < ((uint32_t)((int32_t)(((RuntimeArray*)L_4)->max_length))))))
		{
			goto IL_0034;
		}
	}
	{
		int32_t L_5 = V_1;
		__this->____size = ((int32_t)il2cpp_codegen_add(L_5, 1));
		ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918* L_6 = V_0;
		int32_t L_7 = V_1;
		RuntimeObject* L_8 = ___0_item;
		NullCheck(L_6);
		(L_6)->SetAt(static_cast<il2cpp_array_size_t>(L_7), (RuntimeObject*)L_8);
		return;
	}

IL_0034:
	{
		RuntimeObject* L_9 = ___0_item;
		List_1_AddWithResize_m79A9BF770BEF9C06BE40D5401E55E375F2726CC4(__this, L_9, il2cpp_rgctx_method(method->klass->rgctx_data, 14));
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void List_1_Add_m67EF1200FCD49355EECE77EB754C2EED52F8B3DE_gshared_inline (List_1_tF6085014296AE8ED14C61E2957D735975B6AE6E8* __this, SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121 ___0_item, const RuntimeMethod* method) 
{
	SimSampleU5BU5D_t5841B807530E6C141503B88A570CA4B3CA13A10B* V_0 = NULL;
	int32_t V_1 = 0;
	{
		int32_t L_0 = __this->____version;
		__this->____version = ((int32_t)il2cpp_codegen_add(L_0, 1));
		SimSampleU5BU5D_t5841B807530E6C141503B88A570CA4B3CA13A10B* L_1 = __this->____items;
		V_0 = L_1;
		int32_t L_2 = __this->____size;
		V_1 = L_2;
		int32_t L_3 = V_1;
		SimSampleU5BU5D_t5841B807530E6C141503B88A570CA4B3CA13A10B* L_4 = V_0;
		NullCheck(L_4);
		if ((!(((uint32_t)L_3) < ((uint32_t)((int32_t)(((RuntimeArray*)L_4)->max_length))))))
		{
			goto IL_0034;
		}
	}
	{
		int32_t L_5 = V_1;
		__this->____size = ((int32_t)il2cpp_codegen_add(L_5, 1));
		SimSampleU5BU5D_t5841B807530E6C141503B88A570CA4B3CA13A10B* L_6 = V_0;
		int32_t L_7 = V_1;
		SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121 L_8 = ___0_item;
		NullCheck(L_6);
		(L_6)->SetAt(static_cast<il2cpp_array_size_t>(L_7), (SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121)L_8);
		return;
	}

IL_0034:
	{
		SimSample_t4B97EF3D51F13B80870E3EB5FD539FA52E564121 L_9 = ___0_item;
		List_1_AddWithResize_m8BA79C0F2A6826CAAA21112A30E06104633D95CC(__this, L_9, il2cpp_rgctx_method(method->klass->rgctx_data, 14));
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int64_t BitConverter_DoubleToInt64Bits_m4F42741818550F9956B5FBAF88C051F4DE5B0AE6_inline (double ___0_value, const RuntimeMethod* method) 
{
	{
		int64_t L_0 = *((int64_t*)((uintptr_t)(&___0_value)));
		return L_0;
	}
}
