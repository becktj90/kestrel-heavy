#include "pch-cpp.hpp"






struct Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A;
struct Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8;
struct DelegateU5BU5D_tC5AB7E8F745616680F337909D3A8E6C722CDF771;
struct DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE;
struct IntPtrU5BU5D_tFD177F8C806A6921AD7150264CCC62FA00CAD832;
struct StackTraceU5BU5D_t32FBCB20930EAF5BAE3F450FF75228E5450DA0DF;
struct ArgumentOutOfRangeException_tEA2822DAF62B10EEED00E0E3A341D4BAF78CF85F;
struct Delegate_t;
struct DelegateData_t9B286B493293CD2D23A5B2B5EF0E5B1324C2B77E;
struct GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A;
struct IDictionary_t6D03155AF1FA9083817AA5B6AD7DEEACC26AB220;
struct MethodInfo_t;
struct QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9;
struct SafeSerializationManager_tCBB85B95DFD1634237140CD892E82D06ECB3F5E6;
struct String_t;
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915;

IL2CPP_EXTERN_C RuntimeClass* Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* ArgumentOutOfRangeException_tEA2822DAF62B10EEED00E0E3A341D4BAF78CF85F_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* QualityHold_tD35F47CC53EC283450F095326DB4F7F6210727C9_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C String_t* _stringLiteral5B9FE05484B470B354696B4F06C3B12F71B5BB4A;
IL2CPP_EXTERN_C String_t* _stringLiteral73A0B11BE688B41B0551AD4E388813EBBEB46A09;
IL2CPP_EXTERN_C const RuntimeMethod* Array_Sort_TisDouble_tE150EF3D1D43DEE85D533810AB4C742307EEDE5F_mE54FCE98C4640E01F56530893D1F5AA004A8BE17_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* QualityGovernor__ctor_m5A1225ECE5E6C4AD4C7A4BD1E3CB409ECA0E398C_RuntimeMethod_var;
struct Delegate_t_marshaled_com;
struct Delegate_t_marshaled_pinvoke;
struct Exception_t_marshaled_com;
struct Exception_t_marshaled_pinvoke;

struct DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE;

IL2CPP_EXTERN_C_BEGIN
IL2CPP_EXTERN_C_END

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
struct U3CModuleU3E_t1AE4DAAC990DA9FB7B9C67ED43BF90D751BB94CA 
{
};
struct GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A  : public RuntimeObject
{
	int32_t ___WindowFrames;
	double ___Percentile;
	double ___StepDownMs;
	double ___StepDownHoldSec;
	double ___RecoverMs;
	double ___RecoverHoldSec;
};
struct QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9  : public RuntimeObject
{
	GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A* ____c;
	DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ____ring;
	DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ____scratch;
	int32_t ____count;
	int32_t ____head;
	double ____overSec;
	double ____underSec;
	double ____holdSec;
	bool ___U3CIgnoreFrameTimesU3Ek__BackingField;
	int32_t ___U3CLevelU3Ek__BackingField;
	int32_t ___U3CFloorU3Ek__BackingField;
	int32_t ___U3CCeilingU3Ek__BackingField;
	Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* ___Changed;
};
struct QualityHold_tD35F47CC53EC283450F095326DB4F7F6210727C9  : public RuntimeObject
{
};
struct QualityMath_tE0E0A9330A771073D83F74E807C18CFAFC2A8143  : public RuntimeObject
{
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
struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22 
{
	bool ___m_value;
};
struct Double_tE150EF3D1D43DEE85D533810AB4C742307EEDE5F 
{
	double ___m_value;
};
struct Int32_t680FF22E76F6EFAD4375103CBBFFA0421349384C 
{
	int32_t ___m_value;
};
struct IntPtr_t 
{
	void* ___m_value;
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
struct Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A  : public MulticastDelegate_t
{
};
struct Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8  : public MulticastDelegate_t
{
};
struct ArgumentException_tAD90411542A20A9C72D5CDA3A84181D8B947A263  : public SystemException_tCC48D868298F4C0705279823E34B00F4FBDB7295
{
	String_t* ____paramName;
};
struct ArgumentOutOfRangeException_tEA2822DAF62B10EEED00E0E3A341D4BAF78CF85F  : public ArgumentException_tAD90411542A20A9C72D5CDA3A84181D8B947A263
{
	RuntimeObject* ____actualValue;
};
struct QualityHold_tD35F47CC53EC283450F095326DB4F7F6210727C9_StaticFields
{
	Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* ___Requested;
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


IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Array_Sort_TisDouble_tE150EF3D1D43DEE85D533810AB4C742307EEDE5F_mE54FCE98C4640E01F56530893D1F5AA004A8BE17_gshared (DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___0_array, int32_t ___1_index, int32_t ___2_length, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Action_2_Invoke_m728A2437F181FBC56F4D617249B47F513AC9FC43_gshared_inline (Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* __this, int32_t ___0_arg1, int32_t ___1_arg2, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Action_1_Invoke_m08431FD4B0BCAF1FBC20007E2A950F0259A0A59A_gshared_inline (Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* __this, double ___0_obj, const RuntimeMethod* method) ;

IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2 (RuntimeObject* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Math_Max_m7BAC743E1752A51F258BB82DEBDD13E7C6D3ED26 (double ___0_val1, double ___1_val2, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void QualityGovernor_Reset_m87DB06AC066C2CB18F7EC602BB12E5B1B6DA41BB (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Delegate_t* Delegate_Combine_m1F725AEF318BE6F0426863490691A6F4606E7D00 (Delegate_t* ___0_a, Delegate_t* ___1_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Delegate_t* Delegate_Remove_m8B7DD5661308FA972E23CA1CC3FC9CEB355504E3 (Delegate_t* ___0_source, Delegate_t* ___1_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void GovernorConfig__ctor_m32BC1A5C1D95AB0C92BCE05FBC08B599F30EFD3F (GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void ArgumentOutOfRangeException__ctor_mE5B2755F0BEA043CACF915D5CE140859EE58FA66 (ArgumentOutOfRangeException_tEA2822DAF62B10EEED00E0E3A341D4BAF78CF85F* __this, String_t* ___0_paramName, String_t* ___1_message, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t Math_Max_m530EBA549AFD98CFC2BD29FE86C6376E67DF11CF (int32_t ___0_val1, int32_t ___1_val2, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void QualityGovernor_set_Ceiling_m0B51FB479C3BEC384F5AD8EBEADD5E48C1DAEC3F_inline (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, int32_t ___0_value, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t QualityGovernor_get_Ceiling_m84DC49317C08889E6F2836D5BAA38A71A40EDDDA_inline (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void QualityGovernor_set_Level_mDF6CB5DB875E25978AE849ADDB860CE52C3705D3_inline (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, int32_t ___0_value, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t QualityGovernor_get_Floor_mB9404C20F9F108D8AD2B33C635FB0E3C9688DF3F_inline (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void QualityGovernor_Move_mF077DCCAC82B4504AE1DEE63A16021B1AB201A38 (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, int32_t ___0_to, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Array_Copy_m4233828B4E6288B6D815F539AAA38575DE627900 (RuntimeArray* ___0_sourceArray, RuntimeArray* ___1_destinationArray, int32_t ___2_length, const RuntimeMethod* method) ;
inline void Array_Sort_TisDouble_tE150EF3D1D43DEE85D533810AB4C742307EEDE5F_mE54FCE98C4640E01F56530893D1F5AA004A8BE17 (DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___0_array, int32_t ___1_index, int32_t ___2_length, const RuntimeMethod* method)
{
	((  void (*) (DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE*, int32_t, int32_t, const RuntimeMethod*))Array_Sort_TisDouble_tE150EF3D1D43DEE85D533810AB4C742307EEDE5F_mE54FCE98C4640E01F56530893D1F5AA004A8BE17_gshared)(___0_array, ___1_index, ___2_length, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t Math_Min_m53C488772A34D53917BCA2A491E79A0A5356ED52 (int32_t ___0_val1, int32_t ___1_val2, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool QualityGovernor_get_IgnoreFrameTimes_m854E10F70A93C4FB15F31EC81027A20A133610DD_inline (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double QualityGovernor_get_P95Ms_mA0485367C257A6DE7AD14BD5AF79709F0DC95A63 (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t QualityGovernor_get_Level_m123A996AAFC80D579B2A249537EE63F3569E8E78_inline (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, const RuntimeMethod* method) ;
inline void Action_2_Invoke_m728A2437F181FBC56F4D617249B47F513AC9FC43_inline (Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* __this, int32_t ___0_arg1, int32_t ___1_arg2, const RuntimeMethod* method)
{
	((  void (*) (Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8*, int32_t, int32_t, const RuntimeMethod*))Action_2_Invoke_m728A2437F181FBC56F4D617249B47F513AC9FC43_gshared_inline)(__this, ___0_arg1, ___1_arg2, method);
}
inline void Action_1_Invoke_m08431FD4B0BCAF1FBC20007E2A950F0259A0A59A_inline (Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* __this, double ___0_obj, const RuntimeMethod* method)
{
	((  void (*) (Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A*, double, const RuntimeMethod*))Action_1_Invoke_m08431FD4B0BCAF1FBC20007E2A950F0259A0A59A_gshared_inline)(__this, ___0_obj, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Math_Min_mA3310F1FF7876DA2FC7F37B822E6DD66410565C1 (double ___0_val1, double ___1_val2, const RuntimeMethod* method) ;
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
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void GovernorConfig__ctor_m32BC1A5C1D95AB0C92BCE05FBC08B599F30EFD3F (GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A* __this, const RuntimeMethod* method) 
{
	{
		__this->___WindowFrames = ((int32_t)120);
		__this->___Percentile = (0.94999999999999996);
		__this->___StepDownMs = (20.0);
		__this->___StepDownHoldSec = (3.0);
		__this->___RecoverMs = (16.699999999999999);
		__this->___RecoverHoldSec = (30.0);
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
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool QualityGovernor_get_IgnoreFrameTimes_m854E10F70A93C4FB15F31EC81027A20A133610DD (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, const RuntimeMethod* method) 
{
	{
		bool L_0 = __this->___U3CIgnoreFrameTimesU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void QualityGovernor_set_IgnoreFrameTimes_m6BC857F3A7F49452A4AEE3D88E9FB86E5A21F366 (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, bool ___0_value, const RuntimeMethod* method) 
{
	{
		bool L_0 = ___0_value;
		__this->___U3CIgnoreFrameTimesU3Ek__BackingField = L_0;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double QualityGovernor_get_HoldRemaining_m86F31DBA1951A97BB99F821390AF5640E899914A (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, const RuntimeMethod* method) 
{
	{
		double L_0 = __this->____holdSec;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void QualityGovernor_HoldFor_m4D6EE1F7D270A6F5089FDA662F92BFEE972B077D (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, double ___0_seconds, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		double L_0 = __this->____holdSec;
		double L_1 = ___0_seconds;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_2;
		L_2 = Math_Max_m7BAC743E1752A51F258BB82DEBDD13E7C6D3ED26(L_0, L_1, NULL);
		__this->____holdSec = L_2;
		QualityGovernor_Reset_m87DB06AC066C2CB18F7EC602BB12E5B1B6DA41BB(__this, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t QualityGovernor_get_Level_m123A996AAFC80D579B2A249537EE63F3569E8E78 (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___U3CLevelU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void QualityGovernor_set_Level_mDF6CB5DB875E25978AE849ADDB860CE52C3705D3 (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, int32_t ___0_value, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = ___0_value;
		__this->___U3CLevelU3Ek__BackingField = L_0;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t QualityGovernor_get_Floor_mB9404C20F9F108D8AD2B33C635FB0E3C9688DF3F (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___U3CFloorU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t QualityGovernor_get_Ceiling_m84DC49317C08889E6F2836D5BAA38A71A40EDDDA (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___U3CCeilingU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void QualityGovernor_set_Ceiling_m0B51FB479C3BEC384F5AD8EBEADD5E48C1DAEC3F (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, int32_t ___0_value, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = ___0_value;
		__this->___U3CCeilingU3Ek__BackingField = L_0;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void QualityGovernor_add_Changed_m0D5392468EC400799F32EAB5B136327C58053FF3 (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* ___0_value, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* V_0 = NULL;
	Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* V_1 = NULL;
	Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* V_2 = NULL;
	{
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_0 = __this->___Changed;
		V_0 = L_0;
	}

IL_0007:
	{
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_1 = V_0;
		V_1 = L_1;
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_2 = V_1;
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_3 = ___0_value;
		Delegate_t* L_4;
		L_4 = Delegate_Combine_m1F725AEF318BE6F0426863490691A6F4606E7D00(L_2, L_3, NULL);
		V_2 = ((Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8*)Castclass((RuntimeObject*)L_4, Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8_il2cpp_TypeInfo_var));
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8** L_5 = (Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8**)(&__this->___Changed);
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_6 = V_2;
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_7 = V_1;
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_8;
		L_8 = InterlockedCompareExchangeImpl<Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8*>(L_5, L_6, L_7);
		V_0 = L_8;
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_9 = V_0;
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_10 = V_1;
		if ((!(((RuntimeObject*)(Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8*)L_9) == ((RuntimeObject*)(Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8*)L_10))))
		{
			goto IL_0007;
		}
	}
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void QualityGovernor_remove_Changed_m2676552449C3220354CBF879B2728E30448819C4 (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* ___0_value, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* V_0 = NULL;
	Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* V_1 = NULL;
	Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* V_2 = NULL;
	{
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_0 = __this->___Changed;
		V_0 = L_0;
	}

IL_0007:
	{
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_1 = V_0;
		V_1 = L_1;
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_2 = V_1;
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_3 = ___0_value;
		Delegate_t* L_4;
		L_4 = Delegate_Remove_m8B7DD5661308FA972E23CA1CC3FC9CEB355504E3(L_2, L_3, NULL);
		V_2 = ((Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8*)Castclass((RuntimeObject*)L_4, Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8_il2cpp_TypeInfo_var));
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8** L_5 = (Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8**)(&__this->___Changed);
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_6 = V_2;
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_7 = V_1;
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_8;
		L_8 = InterlockedCompareExchangeImpl<Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8*>(L_5, L_6, L_7);
		V_0 = L_8;
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_9 = V_0;
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_10 = V_1;
		if ((!(((RuntimeObject*)(Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8*)L_9) == ((RuntimeObject*)(Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8*)L_10))))
		{
			goto IL_0007;
		}
	}
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void QualityGovernor__ctor_m5A1225ECE5E6C4AD4C7A4BD1E3CB409ECA0E398C (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A* ___0_c, int32_t ___1_startLevel, int32_t ___2_floor, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A* G_B2_0 = NULL;
	QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* G_B2_1 = NULL;
	GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A* G_B1_0 = NULL;
	QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* G_B1_1 = NULL;
	{
		Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2(__this, NULL);
		GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A* L_0 = ___0_c;
		GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A* L_1 = L_0;
		if (L_1)
		{
			G_B2_0 = L_1;
			G_B2_1 = __this;
			goto IL_0011;
		}
		G_B1_0 = L_1;
		G_B1_1 = __this;
	}
	{
		GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A* L_2 = (GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A*)il2cpp_codegen_object_new(GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A_il2cpp_TypeInfo_var);
		GovernorConfig__ctor_m32BC1A5C1D95AB0C92BCE05FBC08B599F30EFD3F(L_2, NULL);
		G_B2_0 = L_2;
		G_B2_1 = G_B1_1;
	}

IL_0011:
	{
		NullCheck(G_B2_1);
		G_B2_1->____c = G_B2_0;
		Il2CppCodeGenWriteBarrier((void**)(&G_B2_1->____c), (void*)G_B2_0);
		GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A* L_3 = __this->____c;
		NullCheck(L_3);
		int32_t L_4 = L_3->___WindowFrames;
		if ((((int32_t)L_4) >= ((int32_t)8)))
		{
			goto IL_0034;
		}
	}
	{
		ArgumentOutOfRangeException_tEA2822DAF62B10EEED00E0E3A341D4BAF78CF85F* L_5 = (ArgumentOutOfRangeException_tEA2822DAF62B10EEED00E0E3A341D4BAF78CF85F*)il2cpp_codegen_object_new(((RuntimeClass*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&ArgumentOutOfRangeException_tEA2822DAF62B10EEED00E0E3A341D4BAF78CF85F_il2cpp_TypeInfo_var)));
		ArgumentOutOfRangeException__ctor_mE5B2755F0BEA043CACF915D5CE140859EE58FA66(L_5, ((String_t*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&_stringLiteral5B9FE05484B470B354696B4F06C3B12F71B5BB4A)), ((String_t*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&_stringLiteral73A0B11BE688B41B0551AD4E388813EBBEB46A09)), NULL);
		IL2CPP_RAISE_MANAGED_EXCEPTION(L_5, ((RuntimeMethod*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&QualityGovernor__ctor_m5A1225ECE5E6C4AD4C7A4BD1E3CB409ECA0E398C_RuntimeMethod_var)));
	}

IL_0034:
	{
		GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A* L_6 = __this->____c;
		NullCheck(L_6);
		int32_t L_7 = L_6->___WindowFrames;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_8 = (DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE*)(DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE*)SZArrayNew(DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE_il2cpp_TypeInfo_var, (uint32_t)L_7);
		__this->____ring = L_8;
		Il2CppCodeGenWriteBarrier((void**)(&__this->____ring), (void*)L_8);
		GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A* L_9 = __this->____c;
		NullCheck(L_9);
		int32_t L_10 = L_9->___WindowFrames;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_11 = (DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE*)(DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE*)SZArrayNew(DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE_il2cpp_TypeInfo_var, (uint32_t)L_10);
		__this->____scratch = L_11;
		Il2CppCodeGenWriteBarrier((void**)(&__this->____scratch), (void*)L_11);
		int32_t L_12 = ___2_floor;
		__this->___U3CFloorU3Ek__BackingField = L_12;
		int32_t L_13 = ___2_floor;
		int32_t L_14 = ___1_startLevel;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		int32_t L_15;
		L_15 = Math_Max_m530EBA549AFD98CFC2BD29FE86C6376E67DF11CF(L_13, L_14, NULL);
		QualityGovernor_set_Ceiling_m0B51FB479C3BEC384F5AD8EBEADD5E48C1DAEC3F_inline(__this, L_15, NULL);
		int32_t L_16;
		L_16 = QualityGovernor_get_Ceiling_m84DC49317C08889E6F2836D5BAA38A71A40EDDDA_inline(__this, NULL);
		QualityGovernor_set_Level_mDF6CB5DB875E25978AE849ADDB860CE52C3705D3_inline(__this, L_16, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void QualityGovernor_SetChosen_m3B02BD62C90C473055BAC232B22BD597DB9FEF07 (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, int32_t ___0_level, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		int32_t L_0;
		L_0 = QualityGovernor_get_Floor_mB9404C20F9F108D8AD2B33C635FB0E3C9688DF3F_inline(__this, NULL);
		int32_t L_1 = ___0_level;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		int32_t L_2;
		L_2 = Math_Max_m530EBA549AFD98CFC2BD29FE86C6376E67DF11CF(L_0, L_1, NULL);
		QualityGovernor_set_Ceiling_m0B51FB479C3BEC384F5AD8EBEADD5E48C1DAEC3F_inline(__this, L_2, NULL);
		int32_t L_3;
		L_3 = QualityGovernor_get_Ceiling_m84DC49317C08889E6F2836D5BAA38A71A40EDDDA_inline(__this, NULL);
		QualityGovernor_Move_mF077DCCAC82B4504AE1DEE63A16021B1AB201A38(__this, L_3, NULL);
		QualityGovernor_Reset_m87DB06AC066C2CB18F7EC602BB12E5B1B6DA41BB(__this, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void QualityGovernor_Reset_m87DB06AC066C2CB18F7EC602BB12E5B1B6DA41BB (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, const RuntimeMethod* method) 
{
	{
		__this->____count = 0;
		__this->____head = 0;
		__this->____overSec = (0.0);
		__this->____underSec = (0.0);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double QualityGovernor_get_P95Ms_mA0485367C257A6DE7AD14BD5AF79709F0DC95A63 (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Array_Sort_TisDouble_tE150EF3D1D43DEE85D533810AB4C742307EEDE5F_mE54FCE98C4640E01F56530893D1F5AA004A8BE17_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	{
		int32_t L_0 = __this->____count;
		if (L_0)
		{
			goto IL_0012;
		}
	}
	{
		return (0.0);
	}

IL_0012:
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_1 = __this->____ring;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_2 = __this->____scratch;
		int32_t L_3 = __this->____count;
		Array_Copy_m4233828B4E6288B6D815F539AAA38575DE627900((RuntimeArray*)L_1, (RuntimeArray*)L_2, L_3, NULL);
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_4 = __this->____scratch;
		int32_t L_5 = __this->____count;
		Array_Sort_TisDouble_tE150EF3D1D43DEE85D533810AB4C742307EEDE5F_mE54FCE98C4640E01F56530893D1F5AA004A8BE17(L_4, 0, L_5, Array_Sort_TisDouble_tE150EF3D1D43DEE85D533810AB4C742307EEDE5F_mE54FCE98C4640E01F56530893D1F5AA004A8BE17_RuntimeMethod_var);
		GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A* L_6 = __this->____c;
		NullCheck(L_6);
		double L_7 = L_6->___Percentile;
		int32_t L_8 = __this->____count;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_9;
		L_9 = ceil(((double)il2cpp_codegen_multiply(L_7, ((double)L_8))));
		V_0 = ((int32_t)il2cpp_codegen_subtract(il2cpp_codegen_cast_double_to_int<int32_t>(L_9), 1));
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_10 = __this->____scratch;
		int32_t L_11 = __this->____count;
		int32_t L_12 = V_0;
		int32_t L_13;
		L_13 = Math_Min_m53C488772A34D53917BCA2A491E79A0A5356ED52(((int32_t)il2cpp_codegen_subtract(L_11, 1)), L_12, NULL);
		int32_t L_14;
		L_14 = Math_Max_m530EBA549AFD98CFC2BD29FE86C6376E67DF11CF(0, L_13, NULL);
		NullCheck(L_10);
		int32_t L_15 = L_14;
		double L_16 = (L_10)->GetAt(static_cast<il2cpp_array_size_t>(L_15));
		return L_16;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool QualityGovernor_Tick_m16913859C4BCADBE59894FECF350AB31887C232F (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, double ___0_dtSec, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	double V_0 = 0.0;
	{
		double L_0 = ___0_dtSec;
		if ((!(((double)L_0) > ((double)(0.0)))))
		{
			goto IL_0018;
		}
	}
	{
		double L_1 = ___0_dtSec;
		if ((!(((double)L_1) > ((double)(1.0)))))
		{
			goto IL_001a;
		}
	}

IL_0018:
	{
		return (bool)0;
	}

IL_001a:
	{
		bool L_2;
		L_2 = QualityGovernor_get_IgnoreFrameTimes_m854E10F70A93C4FB15F31EC81027A20A133610DD_inline(__this, NULL);
		if (!L_2)
		{
			goto IL_0024;
		}
	}
	{
		return (bool)0;
	}

IL_0024:
	{
		double L_3 = __this->____holdSec;
		if ((!(((double)L_3) > ((double)(0.0)))))
		{
			goto IL_0053;
		}
	}
	{
		double L_4 = __this->____holdSec;
		double L_5 = ___0_dtSec;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_6;
		L_6 = Math_Max_m7BAC743E1752A51F258BB82DEBDD13E7C6D3ED26((0.0), ((double)il2cpp_codegen_subtract(L_4, L_5)), NULL);
		__this->____holdSec = L_6;
		return (bool)0;
	}

IL_0053:
	{
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_7 = __this->____ring;
		int32_t L_8 = __this->____head;
		double L_9 = ___0_dtSec;
		NullCheck(L_7);
		(L_7)->SetAt(static_cast<il2cpp_array_size_t>(L_8), (double)((double)il2cpp_codegen_multiply(L_9, (1000.0))));
		int32_t L_10 = __this->____head;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_11 = __this->____ring;
		NullCheck(L_11);
		__this->____head = ((int32_t)(((int32_t)il2cpp_codegen_add(L_10, 1))%((int32_t)(((RuntimeArray*)L_11)->max_length))));
		int32_t L_12 = __this->____count;
		DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* L_13 = __this->____ring;
		NullCheck(L_13);
		if ((((int32_t)L_12) >= ((int32_t)((int32_t)(((RuntimeArray*)L_13)->max_length)))))
		{
			goto IL_00a2;
		}
	}
	{
		int32_t L_14 = __this->____count;
		__this->____count = ((int32_t)il2cpp_codegen_add(L_14, 1));
		return (bool)0;
	}

IL_00a2:
	{
		double L_15;
		L_15 = QualityGovernor_get_P95Ms_mA0485367C257A6DE7AD14BD5AF79709F0DC95A63(__this, NULL);
		V_0 = L_15;
		double L_16 = V_0;
		GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A* L_17 = __this->____c;
		NullCheck(L_17);
		double L_18 = L_17->___StepDownMs;
		if ((!(((double)L_16) > ((double)L_18))))
		{
			goto IL_00d6;
		}
	}
	{
		double L_19 = __this->____overSec;
		double L_20 = ___0_dtSec;
		__this->____overSec = ((double)il2cpp_codegen_add(L_19, L_20));
		__this->____underSec = (0.0);
		goto IL_0121;
	}

IL_00d6:
	{
		double L_21 = V_0;
		GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A* L_22 = __this->____c;
		NullCheck(L_22);
		double L_23 = L_22->___RecoverMs;
		if ((!(((double)L_21) <= ((double)L_23))))
		{
			goto IL_0103;
		}
	}
	{
		double L_24 = __this->____underSec;
		double L_25 = ___0_dtSec;
		__this->____underSec = ((double)il2cpp_codegen_add(L_24, L_25));
		__this->____overSec = (0.0);
		goto IL_0121;
	}

IL_0103:
	{
		__this->____overSec = (0.0);
		__this->____underSec = (0.0);
	}

IL_0121:
	{
		double L_26 = __this->____overSec;
		GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A* L_27 = __this->____c;
		NullCheck(L_27);
		double L_28 = L_27->___StepDownHoldSec;
		if ((!(((double)L_26) >= ((double)L_28))))
		{
			goto IL_0158;
		}
	}
	{
		int32_t L_29;
		L_29 = QualityGovernor_get_Level_m123A996AAFC80D579B2A249537EE63F3569E8E78_inline(__this, NULL);
		int32_t L_30;
		L_30 = QualityGovernor_get_Floor_mB9404C20F9F108D8AD2B33C635FB0E3C9688DF3F_inline(__this, NULL);
		if ((((int32_t)L_29) <= ((int32_t)L_30)))
		{
			goto IL_0158;
		}
	}
	{
		int32_t L_31;
		L_31 = QualityGovernor_get_Level_m123A996AAFC80D579B2A249537EE63F3569E8E78_inline(__this, NULL);
		QualityGovernor_Move_mF077DCCAC82B4504AE1DEE63A16021B1AB201A38(__this, ((int32_t)il2cpp_codegen_subtract(L_31, 1)), NULL);
		QualityGovernor_Reset_m87DB06AC066C2CB18F7EC602BB12E5B1B6DA41BB(__this, NULL);
		return (bool)1;
	}

IL_0158:
	{
		double L_32 = __this->____underSec;
		GovernorConfig_tA844D33508D6587BCAFB159892EE514E2EFAFB2A* L_33 = __this->____c;
		NullCheck(L_33);
		double L_34 = L_33->___RecoverHoldSec;
		if ((!(((double)L_32) >= ((double)L_34))))
		{
			goto IL_018f;
		}
	}
	{
		int32_t L_35;
		L_35 = QualityGovernor_get_Level_m123A996AAFC80D579B2A249537EE63F3569E8E78_inline(__this, NULL);
		int32_t L_36;
		L_36 = QualityGovernor_get_Ceiling_m84DC49317C08889E6F2836D5BAA38A71A40EDDDA_inline(__this, NULL);
		if ((((int32_t)L_35) >= ((int32_t)L_36)))
		{
			goto IL_018f;
		}
	}
	{
		int32_t L_37;
		L_37 = QualityGovernor_get_Level_m123A996AAFC80D579B2A249537EE63F3569E8E78_inline(__this, NULL);
		QualityGovernor_Move_mF077DCCAC82B4504AE1DEE63A16021B1AB201A38(__this, ((int32_t)il2cpp_codegen_add(L_37, 1)), NULL);
		QualityGovernor_Reset_m87DB06AC066C2CB18F7EC602BB12E5B1B6DA41BB(__this, NULL);
		return (bool)1;
	}

IL_018f:
	{
		return (bool)0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void QualityGovernor_Move_mF077DCCAC82B4504AE1DEE63A16021B1AB201A38 (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, int32_t ___0_to, const RuntimeMethod* method) 
{
	int32_t V_0 = 0;
	Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* G_B4_0 = NULL;
	Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* G_B3_0 = NULL;
	{
		int32_t L_0 = ___0_to;
		int32_t L_1;
		L_1 = QualityGovernor_get_Level_m123A996AAFC80D579B2A249537EE63F3569E8E78_inline(__this, NULL);
		if ((!(((uint32_t)L_0) == ((uint32_t)L_1))))
		{
			goto IL_000a;
		}
	}
	{
		return;
	}

IL_000a:
	{
		int32_t L_2;
		L_2 = QualityGovernor_get_Level_m123A996AAFC80D579B2A249537EE63F3569E8E78_inline(__this, NULL);
		V_0 = L_2;
		int32_t L_3 = ___0_to;
		QualityGovernor_set_Level_mDF6CB5DB875E25978AE849ADDB860CE52C3705D3_inline(__this, L_3, NULL);
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_4 = __this->___Changed;
		Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* L_5 = L_4;
		if (L_5)
		{
			G_B4_0 = L_5;
			goto IL_0023;
		}
		G_B3_0 = L_5;
	}
	{
		return;
	}

IL_0023:
	{
		int32_t L_6 = V_0;
		int32_t L_7 = ___0_to;
		NullCheck(G_B4_0);
		Action_2_Invoke_m728A2437F181FBC56F4D617249B47F513AC9FC43_inline(G_B4_0, L_6, L_7, NULL);
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
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void QualityHold_add_Requested_mD3FD8A1A5021687FDBF4EC5D5FB9A23457CC458D (Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* ___0_value, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&QualityHold_tD35F47CC53EC283450F095326DB4F7F6210727C9_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* V_0 = NULL;
	Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* V_1 = NULL;
	Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* V_2 = NULL;
	{
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_0 = ((QualityHold_tD35F47CC53EC283450F095326DB4F7F6210727C9_StaticFields*)il2cpp_codegen_static_fields_for(QualityHold_tD35F47CC53EC283450F095326DB4F7F6210727C9_il2cpp_TypeInfo_var))->___Requested;
		V_0 = L_0;
	}

IL_0006:
	{
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_1 = V_0;
		V_1 = L_1;
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_2 = V_1;
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_3 = ___0_value;
		Delegate_t* L_4;
		L_4 = Delegate_Combine_m1F725AEF318BE6F0426863490691A6F4606E7D00(L_2, L_3, NULL);
		V_2 = ((Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A*)Castclass((RuntimeObject*)L_4, Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A_il2cpp_TypeInfo_var));
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_5 = V_2;
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_6 = V_1;
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_7;
		L_7 = InterlockedCompareExchangeImpl<Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A*>((&((QualityHold_tD35F47CC53EC283450F095326DB4F7F6210727C9_StaticFields*)il2cpp_codegen_static_fields_for(QualityHold_tD35F47CC53EC283450F095326DB4F7F6210727C9_il2cpp_TypeInfo_var))->___Requested), L_5, L_6);
		V_0 = L_7;
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_8 = V_0;
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_9 = V_1;
		if ((!(((RuntimeObject*)(Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A*)L_8) == ((RuntimeObject*)(Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A*)L_9))))
		{
			goto IL_0006;
		}
	}
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void QualityHold_remove_Requested_m4F59127184BC2DF1A1783621C84B65AF03872E44 (Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* ___0_value, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&QualityHold_tD35F47CC53EC283450F095326DB4F7F6210727C9_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* V_0 = NULL;
	Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* V_1 = NULL;
	Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* V_2 = NULL;
	{
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_0 = ((QualityHold_tD35F47CC53EC283450F095326DB4F7F6210727C9_StaticFields*)il2cpp_codegen_static_fields_for(QualityHold_tD35F47CC53EC283450F095326DB4F7F6210727C9_il2cpp_TypeInfo_var))->___Requested;
		V_0 = L_0;
	}

IL_0006:
	{
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_1 = V_0;
		V_1 = L_1;
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_2 = V_1;
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_3 = ___0_value;
		Delegate_t* L_4;
		L_4 = Delegate_Remove_m8B7DD5661308FA972E23CA1CC3FC9CEB355504E3(L_2, L_3, NULL);
		V_2 = ((Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A*)Castclass((RuntimeObject*)L_4, Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A_il2cpp_TypeInfo_var));
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_5 = V_2;
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_6 = V_1;
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_7;
		L_7 = InterlockedCompareExchangeImpl<Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A*>((&((QualityHold_tD35F47CC53EC283450F095326DB4F7F6210727C9_StaticFields*)il2cpp_codegen_static_fields_for(QualityHold_tD35F47CC53EC283450F095326DB4F7F6210727C9_il2cpp_TypeInfo_var))->___Requested), L_5, L_6);
		V_0 = L_7;
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_8 = V_0;
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_9 = V_1;
		if ((!(((RuntimeObject*)(Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A*)L_8) == ((RuntimeObject*)(Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A*)L_9))))
		{
			goto IL_0006;
		}
	}
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void QualityHold_AtIgnition_mFA7FFE5F7379240E5D91B6D3D3EABD4DA42E8BE9 (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&QualityHold_tD35F47CC53EC283450F095326DB4F7F6210727C9_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* G_B2_0 = NULL;
	Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* G_B1_0 = NULL;
	{
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_0 = ((QualityHold_tD35F47CC53EC283450F095326DB4F7F6210727C9_StaticFields*)il2cpp_codegen_static_fields_for(QualityHold_tD35F47CC53EC283450F095326DB4F7F6210727C9_il2cpp_TypeInfo_var))->___Requested;
		Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* L_1 = L_0;
		if (L_1)
		{
			G_B2_0 = L_1;
			goto IL_000a;
		}
		G_B1_0 = L_1;
	}
	{
		return;
	}

IL_000a:
	{
		NullCheck(G_B2_0);
		Action_1_Invoke_m08431FD4B0BCAF1FBC20007E2A950F0259A0A59A_inline(G_B2_0, (20.0), NULL);
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
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double QualityMath_RenderScale_m128439B44BB878C8005E635722557122D9591731 (double ___0_dprCap, double ___1_dpr, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		double L_0 = ___1_dpr;
		if ((((double)L_0) <= ((double)(0.0))))
		{
			goto IL_002c;
		}
	}
	{
		double L_1 = ___0_dprCap;
		double L_2 = ___1_dpr;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_3;
		L_3 = Math_Min_mA3310F1FF7876DA2FC7F37B822E6DD66410565C1((1.0), ((double)(L_1/L_2)), NULL);
		double L_4;
		L_4 = Math_Max_m7BAC743E1752A51F258BB82DEBDD13E7C6D3ED26((0.10000000000000001), L_3, NULL);
		return L_4;
	}

IL_002c:
	{
		return (1.0);
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void QualityGovernor_set_Ceiling_m0B51FB479C3BEC384F5AD8EBEADD5E48C1DAEC3F_inline (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, int32_t ___0_value, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = ___0_value;
		__this->___U3CCeilingU3Ek__BackingField = L_0;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t QualityGovernor_get_Ceiling_m84DC49317C08889E6F2836D5BAA38A71A40EDDDA_inline (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___U3CCeilingU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void QualityGovernor_set_Level_mDF6CB5DB875E25978AE849ADDB860CE52C3705D3_inline (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, int32_t ___0_value, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = ___0_value;
		__this->___U3CLevelU3Ek__BackingField = L_0;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t QualityGovernor_get_Floor_mB9404C20F9F108D8AD2B33C635FB0E3C9688DF3F_inline (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___U3CFloorU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool QualityGovernor_get_IgnoreFrameTimes_m854E10F70A93C4FB15F31EC81027A20A133610DD_inline (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, const RuntimeMethod* method) 
{
	{
		bool L_0 = __this->___U3CIgnoreFrameTimesU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t QualityGovernor_get_Level_m123A996AAFC80D579B2A249537EE63F3569E8E78_inline (QualityGovernor_t1823BA73C88BC20209CBFBADB567BA75EB2BB2C9* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->___U3CLevelU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Action_2_Invoke_m728A2437F181FBC56F4D617249B47F513AC9FC43_gshared_inline (Action_2_tD7438462601D3939500ED67463331FE00CFFBDB8* __this, int32_t ___0_arg1, int32_t ___1_arg2, const RuntimeMethod* method) 
{
	typedef void (*FunctionPointerType) (RuntimeObject*, int32_t, int32_t, const RuntimeMethod*);
	((FunctionPointerType)__this->___invoke_impl)((Il2CppObject*)__this->___method_code, ___0_arg1, ___1_arg2, reinterpret_cast<RuntimeMethod*>(__this->___method));
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Action_1_Invoke_m08431FD4B0BCAF1FBC20007E2A950F0259A0A59A_gshared_inline (Action_1_t981ADB3D546655C41CBF61B0CB6DC0A30B7BB98A* __this, double ___0_obj, const RuntimeMethod* method) 
{
	typedef void (*FunctionPointerType) (RuntimeObject*, double, const RuntimeMethod*);
	((FunctionPointerType)__this->___invoke_impl)((Il2CppObject*)__this->___method_code, ___0_obj, reinterpret_cast<RuntimeMethod*>(__this->___method));
}
