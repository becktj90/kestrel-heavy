#include "pch-cpp.hpp"





template <typename T1>
struct VirtualActionInvoker1
{
	typedef void (*Action)(void*, T1, const RuntimeMethod*);

	static inline void Invoke (Il2CppMethodSlot slot, RuntimeObject* obj, T1 p1)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_virtual_invoke_data(slot, obj);
		((Action)invokeData.methodPtr)(obj, p1, invokeData.method);
	}
};
template <typename T1, typename T2, typename T3>
struct VirtualActionInvoker3
{
	typedef void (*Action)(void*, T1, T2, T3, const RuntimeMethod*);

	static inline void Invoke (Il2CppMethodSlot slot, RuntimeObject* obj, T1 p1, T2 p2, T3 p3)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_virtual_invoke_data(slot, obj);
		((Action)invokeData.methodPtr)(obj, p1, p2, p3, invokeData.method);
	}
};
template <typename R>
struct VirtualFuncInvoker0
{
	typedef R (*Func)(void*, const RuntimeMethod*);

	static inline R Invoke (Il2CppMethodSlot slot, RuntimeObject* obj)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_virtual_invoke_data(slot, obj);
		return ((Func)invokeData.methodPtr)(obj, invokeData.method);
	}
};
struct InterfaceActionInvoker0
{
	typedef void (*Action)(void*, const RuntimeMethod*);

	static inline void Invoke (Il2CppMethodSlot slot, RuntimeClass* declaringInterface, RuntimeObject* obj)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_interface_invoke_data(slot, obj, declaringInterface);
		((Action)invokeData.methodPtr)(obj, invokeData.method);
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

struct Dictionary_2_t14FE4A752A83D53771C584E4C8D14E01F2AFD7BA;
struct Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A;
struct HashSet_1_t2EC13BE6E93BB0C99D5CF97A25799B40FD6CBAF4;
struct HashSet_1_tA7CAED1C5D01DEEEA947992FCF58776FEDC59BAC;
struct IComparer_1_tC98E0C71B4D27F347C67EAB82A073462E59BCA11;
struct IEnumerable_1_t0AA6705B9378A4033A24C2FB0A50C35A32204416;
struct IEnumerator_1_tD99202564DB6FD9B170E7069409E5370D6604F6B;
struct IEqualityComparer_1_t35CAC4DD92F7A78F1A4376729CBE798858E9DAAB;
struct IEqualityComparer_1_tAE94C8F24AD5B94D4EE85CA9FC59E3409D41CAF7;
struct IReadOnlyList_1_t7BB300FE9C8B0D3BCB34B752D2516BD12EB5E8CF;
struct KeyCollection_t4654C172323F7195AC33E1CFBFB702C70CCA99F0;
struct List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D;
struct List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE;
struct Task_1_t4C228DE57804012969575431CFF12D57C875552D;
struct ValueCollection_tCF1059AD0E67CE920B70F1176BA5D5E993443BE3;
struct EntryU5BU5D_tE34E39D4F8D615289365351AF80928B91221F230;
struct SlotU5BU5D_t14C477364DDDD90C652D9A3DEB56C17B92473359;
struct ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031;
struct CharU5BU5D_t799905CF001DD5F13F7DBB310181FC4D8B7D0AAB;
struct Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C;
struct IntPtrU5BU5D_tFD177F8C806A6921AD7150264CCC62FA00CAD832;
struct ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918;
struct PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F;
struct SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C;
struct StackTraceU5BU5D_t32FBCB20930EAF5BAE3F450FF75228E5450DA0DF;
struct StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248;
struct FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539;
struct VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00;
struct C4U5BU5D_tA0B6DD53ECB5C3687C30C1AE88FBB09AA96AC22B;
struct PrimU5BU5D_t009CCF73481528E4F4C31207D265487A449DD372;
struct DeflateStream_tF1758952E9DBAB2F9A15D42971F33A78AB4FC104;
struct DeflateStreamNative_t06B674E1D2EFD46989197EFB1E33E0B6564793CD;
struct IDictionary_t6D03155AF1FA9083817AA5B6AD7DEEACC26AB220;
struct IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA;
struct MemoryStream_tAAED1B42172E3390584E4194308AB878E786AAC2;
struct NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A;
struct SafeSerializationManager_tCBB85B95DFD1634237140CD892E82D06ECB3F5E6;
struct SemaphoreSlim_t0D5CB5685D9BFA5BF95CEC6E7395490F933E8DB2;
struct SerializationInfo_t3C47F63E24BEB9FCE2DC6309E027F238DC5C5E37;
struct Stream_tF844051B786E8F7F4244DBD218D74E8617B9A2DE;
struct String_t;
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915;
struct FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752;
struct Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD;
struct Job_t0B1165E35BF83E24991BB9F20783AADD55C95267;
struct Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6;
struct ReadWriteTask_t0821BF49EE38596C7734E86E1A6A39D769BE2C05;
struct U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891;

IL2CPP_EXTERN_C RuntimeClass* Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* C4U5BU5D_tA0B6DD53ECB5C3687C30C1AE88FBB09AA96AC22B_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* DeflateStream_tF1758952E9DBAB2F9A15D42971F33A78AB4FC104_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* HashSet_1_tA7CAED1C5D01DEEEA947992FCF58776FEDC59BAC_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IEnumerable_1_t0AA6705B9378A4033A24C2FB0A50C35A32204416_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IEnumerator_1_tD99202564DB6FD9B170E7069409E5370D6604F6B_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Job_t0B1165E35BF83E24991BB9F20783AADD55C95267_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* MemoryStream_tAAED1B42172E3390584E4194308AB878E786AAC2_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeField* U3CPrivateImplementationDetailsU3E_t91B5382CE2346E184773DCFBD60B8EF7D20A968C____4C4B6A3BE1314AB86138BEF4314DDE022E600960D8689A2C8F8631802D20DAB6_FieldInfo_var;
IL2CPP_EXTERN_C RuntimeField* U3CPrivateImplementationDetailsU3E_t91B5382CE2346E184773DCFBD60B8EF7D20A968C____E780A4ABA35D7432F324407DF0FD7E0C6F361D1AD2FFC67E5B9F893DFDD80800_FieldInfo_var;
IL2CPP_EXTERN_C String_t* _stringLiteral00990E38CA33B93B1431473E05022A9DCC677F93;
IL2CPP_EXTERN_C String_t* _stringLiteral1C43EC867F20B8DB8FA13AC94D6DB825367CF26C;
IL2CPP_EXTERN_C String_t* _stringLiteral3485BA793A227DB725EB436AF2FC1354566DEDF7;
IL2CPP_EXTERN_C String_t* _stringLiteral3C4F52AE03EA8FEF5DEAA4BF1DCF2792C970C821;
IL2CPP_EXTERN_C String_t* _stringLiteral3D6DAD0DC8E1D6D4D37111226BD75DCE876A1C7E;
IL2CPP_EXTERN_C String_t* _stringLiteral43AC98067E1CFB50179D46B2DADEF5A10848F8BB;
IL2CPP_EXTERN_C String_t* _stringLiteral4A3A8F16E29B08914BAD480CC20AF8B66E850DAB;
IL2CPP_EXTERN_C String_t* _stringLiteral4BF88E349C859F17645B0DD8922125ABAA3BAA05;
IL2CPP_EXTERN_C String_t* _stringLiteral52A70EE7ABEE0DD947480638E3B909A9A19C1D4E;
IL2CPP_EXTERN_C String_t* _stringLiteral9053C7BE1B5533FB96F3DE70C35EA0FFAB4FE19E;
IL2CPP_EXTERN_C String_t* _stringLiteral91A69CE9B7240FE9A06FD5FC7D58A6D3255669B7;
IL2CPP_EXTERN_C String_t* _stringLiteralA9F5008AA90307FD7521614307E34133396662CA;
IL2CPP_EXTERN_C String_t* _stringLiteralBF8DB1FCA3176EF0EAAE60DA02213D8D74498AFE;
IL2CPP_EXTERN_C String_t* _stringLiteralD3CB15C4F0E233957BA9ED742AFB0EBF860C118F;
IL2CPP_EXTERN_C String_t* _stringLiteralE7B97EE372F4025D4538E7D1797CE1F0D6F0409C;
IL2CPP_EXTERN_C const RuntimeMethod* Array_Resize_TisByte_t94D9231AC217BE4D2E004C4CD32DF6D099EA41A3_mDEA5A67641D397AE82678232B46E377173ED4297_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Array_Sort_TisFace_tDD2362557504D5249B24936FC555B3D54E56E6BE_m0D4A3A8826843727533FDA0D624989B397237008_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Dictionary_2_TryGetValue_m269D507F7E00AC2566CC52C93E3E3A084A03B5F6_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Dictionary_2__ctor_m432862A4D3846A8EC1E05271D1CA0346C288B93D_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Dictionary_2_set_Item_mB2347972BD237E936BF2A0F5921FF0335524879C_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Enumerator_Dispose_mF1558E7B7B84D786841C7B736ECC77BEED4B1367_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Enumerator_MoveNext_mD22EC89FDB16CDABC6AB475A8E71C56858023AC5_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Enumerator_get_Current_m7673828665C413CFC1943B4A6DB0F0148D99596B_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* HashSet_1_Add_m4351A844FAB44D68332AEF82045AE45A6340E64C_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* HashSet_1__ctor_m15C5D34CE1AC392262EDDB77250F68CEF3522CF4_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1_Add_mE2377BD05FECB1E7EA1473B773D581201C95A728_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1_GetEnumerator_mCD2F70F8886ECEF58C2B0ACD4CC85F99A80733B7_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1__ctor_mCB861F16F94DDD8BB0B8397D76C21FEE49D42687_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1_get_Count_mA4B6A7483D74113D7978E2DA5D1DEE4F906ECC23_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1_get_Item_m658740103DB2DE2E2FF16DEFD463120C34BBE346_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* U3CCornersU3Ed__18_System_Collections_IEnumerator_Reset_m8965145E01DE54472727F2AAFC07B18E8B9E5CD5_RuntimeMethod_var;
struct Exception_t_marshaled_com;
struct Exception_t_marshaled_pinvoke;

struct ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031;
struct Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C;
struct ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918;
struct PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F;
struct SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C;
struct StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248;
struct FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539;
struct VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00;
struct C4U5BU5D_tA0B6DD53ECB5C3687C30C1AE88FBB09AA96AC22B;

IL2CPP_EXTERN_C_BEGIN
IL2CPP_EXTERN_C_END

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
struct Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A  : public RuntimeObject
{
	Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* ____buckets;
	EntryU5BU5D_tE34E39D4F8D615289365351AF80928B91221F230* ____entries;
	int32_t ____count;
	int32_t ____freeList;
	int32_t ____freeCount;
	int32_t ____version;
	RuntimeObject* ____comparer;
	KeyCollection_t4654C172323F7195AC33E1CFBFB702C70CCA99F0* ____keys;
	ValueCollection_tCF1059AD0E67CE920B70F1176BA5D5E993443BE3* ____values;
	RuntimeObject* ____syncRoot;
};
struct HashSet_1_tA7CAED1C5D01DEEEA947992FCF58776FEDC59BAC  : public RuntimeObject
{
	Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* ____buckets;
	SlotU5BU5D_t14C477364DDDD90C652D9A3DEB56C17B92473359* ____slots;
	int32_t ____count;
	int32_t ____lastIndex;
	int32_t ____freeList;
	RuntimeObject* ____comparer;
	int32_t ____version;
	SerializationInfo_t3C47F63E24BEB9FCE2DC6309E027F238DC5C5E37* ____siInfo;
};
struct List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D  : public RuntimeObject
{
	ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918* ____items;
	int32_t ____size;
	int32_t ____version;
	RuntimeObject* ____syncRoot;
};
struct List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE  : public RuntimeObject
{
	PrimU5BU5D_t009CCF73481528E4F4C31207D265487A449DD372* ____items;
	int32_t ____size;
	int32_t ____version;
	RuntimeObject* ____syncRoot;
};
struct MarshalByRefObject_t8C2F4C5854177FD60439EB1FCCFC1B3CFAFE8DCE  : public RuntimeObject
{
	RuntimeObject* ____identity;
};
struct MarshalByRefObject_t8C2F4C5854177FD60439EB1FCCFC1B3CFAFE8DCE_marshaled_pinvoke
{
	Il2CppIUnknown* ____identity;
};
struct MarshalByRefObject_t8C2F4C5854177FD60439EB1FCCFC1B3CFAFE8DCE_marshaled_com
{
	Il2CppIUnknown* ____identity;
};
struct PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D  : public RuntimeObject
{
};
struct PayloadPortrait_t853002ADFCE7DA5EC38345E949481772D5DFF37B  : public RuntimeObject
{
};
struct PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC  : public RuntimeObject
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
struct FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752  : public RuntimeObject
{
};
struct Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD  : public RuntimeObject
{
	List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* ___Parts;
};
struct Enumerator_t9473BAB568A27E2339D48C1F91319E0F6D244D7A 
{
	List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D* ____list;
	int32_t ____index;
	int32_t ____version;
	RuntimeObject* ____current;
};
struct Enumerator_t785E35998B1974B111A16DAFC5F62FCDEC615835 
{
	List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* ____list;
	int32_t ____index;
	int32_t ____version;
	Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* ____current;
};
struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22 
{
	bool ___m_value;
};
struct Byte_t94D9231AC217BE4D2E004C4CD32DF6D099EA41A3 
{
	uint8_t ___m_value;
};
struct Char_t521A6F19B456D956AF452D926C32709DC03D6B17 
{
	Il2CppChar ___m_value;
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
struct PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E 
{
	float ___X;
	float ___Y;
	float ___Z;
	float ___W;
	float ___H;
	float ___D;
	uint8_t ___R;
	uint8_t ___G;
	uint8_t ___B;
};
struct Single_t4530F2FF86FCB0DC29F35385CA1BD21BE294761C 
{
	float ___m_value;
};
struct Stream_tF844051B786E8F7F4244DBD218D74E8617B9A2DE  : public MarshalByRefObject_t8C2F4C5854177FD60439EB1FCCFC1B3CFAFE8DCE
{
	ReadWriteTask_t0821BF49EE38596C7734E86E1A6A39D769BE2C05* ____activeReadWriteTask;
	SemaphoreSlim_t0D5CB5685D9BFA5BF95CEC6E7395490F933E8DB2* ____asyncActiveSemaphore;
};
struct UInt32_t1833D51FFA667B18A5AA4B8D34DE284F8495D29B 
{
	uint32_t ___m_value;
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
struct Face_tDD2362557504D5249B24936FC555B3D54E56E6BE 
{
	int32_t ___A;
	int32_t ___B;
	int32_t ___C;
	int32_t ___D;
	float ___Z;
	float ___Shade;
	uint8_t ___Cr;
	uint8_t ___Cg;
	uint8_t ___Cb;
};
struct Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 
{
	float ___X;
	float ___Y;
	float ___Z;
};
struct C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F 
{
	float ___R;
	float ___G;
	float ___B;
	float ___A;
};
struct V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D 
{
	float ___X;
	float ___Y;
	float ___Z;
};
struct CompressionLevel_t6077A887918BD6DACDD8D3F0D660D157A604B1E1 
{
	int32_t ___value__;
};
struct CompressionMode_t840C8957CF84ACFBF5C43E48AEAEB9DB5E64F3C5 
{
	int32_t ___value__;
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
struct Int32Enum_tCBAC8BA2BFF3A845FA599F303093BBBA374B6F0C 
{
	int32_t ___value__;
};
struct MemoryStream_tAAED1B42172E3390584E4194308AB878E786AAC2  : public Stream_tF844051B786E8F7F4244DBD218D74E8617B9A2DE
{
	ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ____buffer;
	int32_t ____origin;
	int32_t ____position;
	int32_t ____length;
	int32_t ____capacity;
	bool ____expandable;
	bool ____writable;
	bool ____exposable;
	bool ____isOpen;
	Task_1_t4C228DE57804012969575431CFF12D57C875552D* ____lastReadTask;
};
struct PayloadBackdrop_tB5CC55E1E80844A38D0E00DF52C1364001AFDFD4 
{
	int32_t ___value__;
};
struct PayloadSurface_t777862D7F04CBB2975356F28C0B1E916925D0726 
{
	int32_t ___value__;
};
struct RuntimeFieldHandle_t6E4C45B6D2EA12FC99185805A7E77527899B25C5 
{
	intptr_t ___value;
};
struct Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 
{
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___Eye;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___F;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___R;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___U;
	float ___X0;
	float ___Y0;
	float ___Span;
};
struct Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 
{
	int32_t ___Index;
	float ___T;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___P;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___N;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___Local;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___LocalN;
};
struct Kind_t6378A226B0E25BDF362F261AC8E56DB86FE6F82E 
{
	int32_t ___value__;
};
struct Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 
{
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___Albedo;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___Spec;
	float ___Diffuse;
	float ___SpecK;
	float ___Power;
	float ___Env;
	float ___Metal;
};
struct U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891  : public RuntimeObject
{
	int32_t ___U3CU3E1__state;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___U3CU3E2__current;
	int32_t ___U3CU3El__initialThreadId;
	Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* ___U3CU3E4__this;
	float ___U3CexU3E5__2;
	float ___U3CeyU3E5__3;
	int32_t ___U3CiU3E5__4;
};
struct DeflateStream_tF1758952E9DBAB2F9A15D42971F33A78AB4FC104  : public Stream_tF844051B786E8F7F4244DBD218D74E8617B9A2DE
{
	Stream_tF844051B786E8F7F4244DBD218D74E8617B9A2DE* ___base_stream;
	int32_t ___mode;
	bool ___leaveOpen;
	bool ___disposed;
	DeflateStreamNative_t06B674E1D2EFD46989197EFB1E33E0B6564793CD* ___native;
};
struct SystemException_tCC48D868298F4C0705279823E34B00F4FBDB7295  : public Exception_t
{
};
struct Job_t0B1165E35BF83E24991BB9F20783AADD55C95267  : public RuntimeObject
{
	List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* ____scene;
	Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 ____cam;
	int32_t ____backdrop;
	Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* ____ids;
	C4U5BU5D_tA0B6DD53ECB5C3687C30C1AE88FBB09AA96AC22B* ____col;
	int32_t ____row;
	int32_t ___Px;
	String_t* ___Form;
	ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___Pixels;
};
struct Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6  : public RuntimeObject
{
	int32_t ___Kind;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___O;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___U;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___V;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___W;
	float ___A;
	float ___B;
	float ___C;
	float ___Z0;
	float ___Z1;
	int32_t ___Surface;
	float ___Seed;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___Center;
	float ___Radius;
};
struct NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A  : public SystemException_tCC48D868298F4C0705279823E34B00F4FBDB7295
{
};
struct List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D_StaticFields
{
	ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918* ___s_emptyArray;
};
struct List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE_StaticFields
{
	PrimU5BU5D_t009CCF73481528E4F4C31207D265487A449DD372* ___s_emptyArray;
};
struct PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_StaticFields
{
	StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* ___Known;
};
struct PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields
{
	SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* ___Rgss;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___ViewDir;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___KeyDir;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___RimDir;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___KeyCol;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___RimCol;
	Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A* ___Scenes;
};
struct String_t_StaticFields
{
	String_t* ___Empty;
};
struct FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752_StaticFields
{
	FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752* ___FarFirst;
};
struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22_StaticFields
{
	String_t* ___TrueString;
	String_t* ___FalseString;
};
struct Char_t521A6F19B456D956AF452D926C32709DC03D6B17_StaticFields
{
	ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___s_categoryForLatin1;
};
struct Stream_tF844051B786E8F7F4244DBD218D74E8617B9A2DE_StaticFields
{
	Stream_tF844051B786E8F7F4244DBD218D74E8617B9A2DE* ___Null;
};
#ifdef __clang__
#pragma clang diagnostic pop
#endif
struct StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248  : public RuntimeArray
{
	ALIGN_FIELD (8) String_t* m_Items[1];

	inline String_t* GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline String_t** GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, String_t* value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
		Il2CppCodeGenWriteBarrier((void**)m_Items + index, (void*)value);
	}
	inline String_t* GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline String_t** GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, String_t* value)
	{
		m_Items[index] = value;
		Il2CppCodeGenWriteBarrier((void**)m_Items + index, (void*)value);
	}
};
struct PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F  : public RuntimeArray
{
	ALIGN_FIELD (8) PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E m_Items[1];

	inline PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E* GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
	}
	inline PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E* GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E value)
	{
		m_Items[index] = value;
	}
};
struct ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031  : public RuntimeArray
{
	ALIGN_FIELD (8) uint8_t m_Items[1];

	inline uint8_t GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline uint8_t* GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, uint8_t value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
	}
	inline uint8_t GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline uint8_t* GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, uint8_t value)
	{
		m_Items[index] = value;
	}
};
struct SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C  : public RuntimeArray
{
	ALIGN_FIELD (8) float m_Items[1];

	inline float GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline float* GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, float value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
	}
	inline float GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline float* GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, float value)
	{
		m_Items[index] = value;
	}
};
struct VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00  : public RuntimeArray
{
	ALIGN_FIELD (8) Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 m_Items[1];

	inline Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109* GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
	}
	inline Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109* GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 value)
	{
		m_Items[index] = value;
	}
};
struct FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539  : public RuntimeArray
{
	ALIGN_FIELD (8) Face_tDD2362557504D5249B24936FC555B3D54E56E6BE m_Items[1];

	inline Face_tDD2362557504D5249B24936FC555B3D54E56E6BE GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline Face_tDD2362557504D5249B24936FC555B3D54E56E6BE* GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, Face_tDD2362557504D5249B24936FC555B3D54E56E6BE value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
	}
	inline Face_tDD2362557504D5249B24936FC555B3D54E56E6BE GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline Face_tDD2362557504D5249B24936FC555B3D54E56E6BE* GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, Face_tDD2362557504D5249B24936FC555B3D54E56E6BE value)
	{
		m_Items[index] = value;
	}
};
struct Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C  : public RuntimeArray
{
	ALIGN_FIELD (8) int32_t m_Items[1];

	inline int32_t GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline int32_t* GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, int32_t value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
	}
	inline int32_t GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline int32_t* GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, int32_t value)
	{
		m_Items[index] = value;
	}
};
struct C4U5BU5D_tA0B6DD53ECB5C3687C30C1AE88FBB09AA96AC22B  : public RuntimeArray
{
	ALIGN_FIELD (8) C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F m_Items[1];

	inline C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F* GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
	}
	inline C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F* GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F value)
	{
		m_Items[index] = value;
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


IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Array_Sort_TisFace_tDD2362557504D5249B24936FC555B3D54E56E6BE_m0D4A3A8826843727533FDA0D624989B397237008_gshared (FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539* ___0_array, int32_t ___1_index, int32_t ___2_length, RuntimeObject* ___3_comparer, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Array_Resize_TisByte_t94D9231AC217BE4D2E004C4CD32DF6D099EA41A3_mDEA5A67641D397AE82678232B46E377173ED4297_gshared (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031** ___0_array, int32_t ___1_newSize, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Enumerator_t9473BAB568A27E2339D48C1F91319E0F6D244D7A List_1_GetEnumerator_mD8294A7FA2BEB1929487127D476F8EC1CDC23BFC_gshared (List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Enumerator_Dispose_mD9DC3E3C3697830A4823047AB29A77DBBB5ED419_gshared (Enumerator_t9473BAB568A27E2339D48C1F91319E0F6D244D7A* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR RuntimeObject* Enumerator_get_Current_m6330F15D18EE4F547C05DF9BF83C5EB710376027_gshared_inline (Enumerator_t9473BAB568A27E2339D48C1F91319E0F6D244D7A* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Enumerator_MoveNext_mE921CC8F29FBBDE7CC3209A0ED0D921D58D00BCB_gshared (Enumerator_t9473BAB568A27E2339D48C1F91319E0F6D244D7A* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* List_1_get_Item_m33561245D64798C2AB07584C0EC4F240E4839A38_gshared (List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D* __this, int32_t ___0_index, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t List_1_get_Count_m4407E4C389F22B8CEC282C15D56516658746C383_gshared_inline (List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Dictionary_2_TryGetValue_mD15380A4ED7CDEE99EA45881577D26BA9CE1B849_gshared (Dictionary_2_t14FE4A752A83D53771C584E4C8D14E01F2AFD7BA* __this, RuntimeObject* ___0_key, RuntimeObject** ___1_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Dictionary_2_set_Item_m1A840355E8EDAECEA9D0C6F5E51B248FAA449CBD_gshared (Dictionary_2_t14FE4A752A83D53771C584E4C8D14E01F2AFD7BA* __this, RuntimeObject* ___0_key, RuntimeObject* ___1_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void HashSet_1__ctor_mD2BBE225041537B7240E453D3E14991EB2169E2C_gshared (HashSet_1_t2EC13BE6E93BB0C99D5CF97A25799B40FD6CBAF4* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool HashSet_1_Add_m4F8BBB0C65C72E41F10A7F83E2963B71A631153C_gshared (HashSet_1_t2EC13BE6E93BB0C99D5CF97A25799B40FD6CBAF4* __this, int32_t ___0_item, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Dictionary_2__ctor_m5B32FBC624618211EB461D59CFBB10E987FD1329_gshared (Dictionary_2_t14FE4A752A83D53771C584E4C8D14E01F2AFD7BA* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void List_1_Add_mEBCF994CC3814631017F46A387B1A192ED6C85C7_gshared_inline (List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D* __this, RuntimeObject* ___0_item, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void List_1__ctor_m7F078BB342729BDF11327FD89D7872265328F690_gshared (List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_NO_INLINE IL2CPP_METHOD_ATTR void List_1_AddWithResize_m79A9BF770BEF9C06BE40D5401E55E375F2726CC4_gshared (List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D* __this, RuntimeObject* ___0_item, const RuntimeMethod* method) ;

IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadPart__ctor_mCCEF62D79BA5DFB74637E8579AC4049EDC15D60B (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E* __this, float ___0_x, float ___1_y, float ___2_z, float ___3_w, float ___4_h, float ___5_d, uint8_t ___6_r, uint8_t ___7_g, uint8_t ___8_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool String_IsNullOrEmpty_mEA9E3FB005AC28FE02E69FCF95A7B8456192B478 (String_t* ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1 (String_t* ___0_a, String_t* ___1_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t U3CPrivateImplementationDetailsU3E_ComputeStringHash_mC32B6AAD7FBB5563CF84923C7FCAA71C251E5DF0 (String_t* ___0_s, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Cubesat_mD6939CF7FF6BB701A4D626A996D5623706C80C4A (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Bus_m018B3D5C17DBDE8D0F2D3F577D92D5550812322E (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Weather_m713ADEFFED4DABB3A7202A90D75DCC6332A01E05 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Comms_m9946E87C9DCB244BC2D9E6BA776D6A1AA7D33CA2 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Telescope_mE851270AD4FE36D4BF45AD4209F2952F89E6C8A7 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Probe_mC48A958CAC88EA1874BAF7A28B262321B9A715DC (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Cargo_m8C42096FDB79860910E259E4899B2F1641B5701A (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Crew_m4F7B1020ADA2118B5155504E8B0F9B5969BDF437 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Dispenser_m0824F74D9326461B9E6EDD6CA389B1C5CF0E9DBA (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Relay_m8610571C9500530B6CB4B612C8285A0D2AE63EA4 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Lander_mE514D3383B00B13820AF571557D4C61F54E4D592 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Box_mF168EBD81D889520193BADD0F403E968FABDC729 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593 (float ___0_x, float ___1_y, float ___2_z, float ___3_w, float ___4_h, float ___5_d, uint8_t ___6_r, uint8_t ___7_g, uint8_t ___8_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Of_mC9B287472F9321C9F04A6C3D600C62ADF1E7EC19 (String_t* ___0_id, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC (Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109* __this, float ___0_x, float ___1_y, float ___2_z, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 PayloadPortrait_Norm_m29C7521F65F744765AAD755AB48176D383F5C4DE (Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___0_v, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 PayloadPortrait_Cross_mE37122B76B6C8A2998DB6F4AADC3B52E868C18FA (Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___0_a, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___1_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadPortrait_Corners_m984E1DD5DB4B7C3BABC0A4D9F571ADF905597947 (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E ___0_p, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___1_eye, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___2_right, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___3_up, VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* ___4_proj, int32_t ___5_at, float* ___6_minX, float* ___7_minY, float* ___8_maxX, float* ___9_maxY, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA (float ___0_val1, float ___1_val2, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadPortrait_AddFace_m868C5E06CDDD3189CE2766E38EF3B0F733EF56F6 (FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539* ___0_faces, int32_t* ___1_n, VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* ___2_proj, int32_t ___3_b, int32_t ___4_a, int32_t ___5_c, int32_t ___6_d, int32_t ___7_e, PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E ___8_part, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___9_light, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___10_normal, const RuntimeMethod* method) ;
inline void Array_Sort_TisFace_tDD2362557504D5249B24936FC555B3D54E56E6BE_m0D4A3A8826843727533FDA0D624989B397237008 (FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539* ___0_array, int32_t ___1_index, int32_t ___2_length, RuntimeObject* ___3_comparer, const RuntimeMethod* method)
{
	((  void (*) (FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539*, int32_t, int32_t, RuntimeObject*, const RuntimeMethod*))Array_Sort_TisFace_tDD2362557504D5249B24936FC555B3D54E56E6BE_m0D4A3A8826843727533FDA0D624989B397237008_gshared)(___0_array, ___1_index, ___2_length, ___3_comparer, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 PayloadPortrait_Screen_m3A69D4B7C548A9DB2FFD5B88FD035AD5044CF3F5 (Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___0_p, float ___1_cx, float ___2_cy, float ___3_fit, int32_t ___4_size, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadPortrait_Tri_m7F91E4A7D1249290E59C4093A730EA62BB195A43 (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___0_pix, SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* ___1_depth, int32_t ___2_size, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___3_a, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___4_b, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___5_c, float ___6_z, uint8_t ___7_r, uint8_t ___8_g, uint8_t ___9_bl, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadPortrait_Ink_m83FD502D90533537A20F3C82A96124A00EBCBF60 (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___0_pix, int32_t ___1_size, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* PayloadPortrait_Rgba_mE697FE81F534A6E3DB3652B4B0FC265EED3D5B81 (String_t* ___0_form, int32_t ___1_size, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* PayloadPortrait_EncodePng_m70BAD5C2F9C8A28CF61592C8CC058E3EFEF2DC08 (int32_t ___0_w, int32_t ___1_h, ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___2_rgba, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Buffer_BlockCopy_m2F7BC0C5BA97C500E3F87D5008718F797E02B358 (RuntimeArray* ___0_src, int32_t ___1_srcOffset, RuntimeArray* ___2_dst, int32_t ___3_dstOffset, int32_t ___4_count, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* PayloadPortrait_Zlib_mBDCD2A945DE849A569570D76237699D522FB2A28 (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___0_raw, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RuntimeHelpers_InitializeArray_m751372AA3F24FBF6DA9B9D687CBFA2DE436CAB9B (RuntimeArray* ___0_array, RuntimeFieldHandle_t6E4C45B6D2EA12FC99185805A7E77527899B25C5 ___1_fldHandle, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* PayloadPortrait_Ihdr_mA24744E56AEA81D70DBD8289B77A595EB1A07C16 (int32_t ___0_w, int32_t ___1_h, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t PayloadPortrait_Chunk_m41C41C7B18F101C865A33CEA318FB20276027F44 (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___0_png, int32_t ___1_o, String_t* ___2_type, ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___3_data, const RuntimeMethod* method) ;
inline void Array_Resize_TisByte_t94D9231AC217BE4D2E004C4CD32DF6D099EA41A3_mDEA5A67641D397AE82678232B46E377173ED4297 (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031** ___0_array, int32_t ___1_newSize, const RuntimeMethod* method)
{
	((  void (*) (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031**, int32_t, const RuntimeMethod*))Array_Resize_TisByte_t94D9231AC217BE4D2E004C4CD32DF6D099EA41A3_mDEA5A67641D397AE82678232B46E377173ED4297_gshared)(___0_array, ___1_newSize, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadPortrait_Dot_m3B2C1EFC28B630AD386621635E287771DB77CA7E (Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___0_a, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___1_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float Math_Min_mE913811A2F7566294BF4649A434282634E7254B3 (float ___0_val1, float ___1_val2, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadPortrait_Edge_mE49BB0134C81E064C96AB56D74C59311CF278171 (Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___0_a, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___1_b, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___2_p, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* Array_Clone_m66C9D0727C9BAA0995E4142F29B45BC03582E042 (RuntimeArray* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadPortrait_Be_m64E1989BA42015D7FC4F7A554D8466B6137111E4 (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___0_b, int32_t ___1_o, int32_t ___2_v, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Il2CppChar String_get_Chars_mC49DF0CD2D3BE7BE97B3AD9C995BE3094F8E36D3 (String_t* __this, int32_t ___0_index, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t PayloadPortrait_Crc_m6805124043E0DD4FE8BF6B74295A01D263347692 (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___0_data, int32_t ___1_start, int32_t ___2_len, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void MemoryStream__ctor_m8F3BAE0B48E65BAA13C52FB020E502B3EA22CA6B (MemoryStream_tAAED1B42172E3390584E4194308AB878E786AAC2* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void DeflateStream__ctor_m55E861339218B06FD5B41D4DCAF09E598AF0C074 (DeflateStream_tF1758952E9DBAB2F9A15D42971F33A78AB4FC104* __this, Stream_tF844051B786E8F7F4244DBD218D74E8617B9A2DE* ___0_stream, int32_t ___1_compressionLevel, bool ___2_leaveOpen, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t PayloadPortrait_Adler_m6D2D1A1F00857BA4BB3740C4C63916E5CB36A740 (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___0_data, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t Single_CompareTo_m06F7868162EB392D3E99103D1A0BD27463C9E66F (float* __this, float ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2 (RuntimeObject* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void FaceOrder__ctor_mF221956DD9BEBFB416D9DB8BF0E77B2CF1F6B04E (FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Job__ctor_m89419F39EB941FDAA2D4CA1E62E8EE9C3A0FE7EE (Job_t0B1165E35BF83E24991BB9F20783AADD55C95267* __this, String_t* ___0_form, int32_t ___1_px, int32_t ___2_backdrop, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR RuntimeObject* PayloadForms_get_Ids_mC28791119EB2FF88D4CDBDB1C881C351BDC5E17E_inline (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Job_Step_mA354313D818FA7693E6076FBB886FB20772E675A (Job_t0B1165E35BF83E24991BB9F20783AADD55C95267* __this, int32_t ___0_rows, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t PayloadRender_Clamp_m031B01B75934395207D92A93B1208E9EA1905F81 (int32_t ___0_v, int32_t ___1_lo, int32_t ___2_hi, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* PayloadRender_Rgba_mBF3297E7D7F7F9B9A105BFA1EFC58C29381CBED2 (String_t* ___0_form, int32_t ___1_px, int32_t ___2_backdrop, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Math_Max_m7BAC743E1752A51F258BB82DEBDD13E7C6D3ED26 (double ___0_val1, double ___1_val2, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* PayloadRender_Scene_mF202D14509424834B06B24444ADF5AC9BCD5B49A (String_t* ___0_form, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 PayloadRender_Frame_mACB603D0B07928A49BEDD4335DC3A98C01CB8196 (List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* ___0_scene, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F PayloadRender_Sample_mE7A4F8364A54709D1A58A600D4D029ED4B123C4A (List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* ___0_scene, Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 ___1_cam, float ___2_u, float ___3_v, int32_t ___4_backdrop, int32_t* ___5_id, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* __this, float ___0_x, float ___1_y, float ___2_z, const RuntimeMethod* method) ;
inline Enumerator_t785E35998B1974B111A16DAFC5F62FCDEC615835 List_1_GetEnumerator_mCD2F70F8886ECEF58C2B0ACD4CC85F99A80733B7 (List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* __this, const RuntimeMethod* method)
{
	return ((  Enumerator_t785E35998B1974B111A16DAFC5F62FCDEC615835 (*) (List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE*, const RuntimeMethod*))List_1_GetEnumerator_mD8294A7FA2BEB1929487127D476F8EC1CDC23BFC_gshared)(__this, method);
}
inline void Enumerator_Dispose_mF1558E7B7B84D786841C7B736ECC77BEED4B1367 (Enumerator_t785E35998B1974B111A16DAFC5F62FCDEC615835* __this, const RuntimeMethod* method)
{
	((  void (*) (Enumerator_t785E35998B1974B111A16DAFC5F62FCDEC615835*, const RuntimeMethod*))Enumerator_Dispose_mD9DC3E3C3697830A4823047AB29A77DBBB5ED419_gshared)(__this, method);
}
inline Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* Enumerator_get_Current_m7673828665C413CFC1943B4A6DB0F0148D99596B_inline (Enumerator_t785E35998B1974B111A16DAFC5F62FCDEC615835* __this, const RuntimeMethod* method)
{
	return ((  Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* (*) (Enumerator_t785E35998B1974B111A16DAFC5F62FCDEC615835*, const RuntimeMethod*))Enumerator_get_Current_m6330F15D18EE4F547C05DF9BF83C5EB710376027_gshared_inline)(__this, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_op_Subtraction_mBF754B8B036EFB8C8B32E2C6A9799AFC1B245482 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_Min_mD303066D8BA6D62D502E1D8BD1B22E60AD952D53 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_Max_m0E6A19FFD4652EF0A8D51250B6A5D96A07E08925 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_b, const RuntimeMethod* method) ;
inline bool Enumerator_MoveNext_mD22EC89FDB16CDABC6AB475A8E71C56858023AC5 (Enumerator_t785E35998B1974B111A16DAFC5F62FCDEC615835* __this, const RuntimeMethod* method)
{
	return ((  bool (*) (Enumerator_t785E35998B1974B111A16DAFC5F62FCDEC615835*, const RuntimeMethod*))Enumerator_MoveNext_mE921CC8F29FBBDE7CC3209A0ED0D921D58D00BCB_gshared)(__this, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, float ___1_s, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float V3_get_Length_mE87BCCE258FFBA7CC979830CD4B263B2BD63B9DE (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_Cross_m88C432D75F76D3BCA973DF38C923E355B02048F3 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* Prim_Corners_m512FD0FEA6F7C961CB55E658A3D7053C4700C542 (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool PayloadRender_Trace_m537188514DC0B279D080B22F51DB8A5234E6FD59 (List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* ___0_scene, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_o, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___2_d, float ___3_maxT, int32_t ___4_skip, Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642* ___5_best, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D PayloadRender_Backdrop_mFF5B7FEABCD4EB45875518068136093EB183F09D (float ___0_u, float ___1_v, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D PayloadRender_Shade_m6993732C766049B3B2E5703DB6C41C29238C3975 (List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* ___0_scene, Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 ___1_h, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___2_dir, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_Lerp_mB6067E0973AEFE35323954D146ED580F05E2D125 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_b, float ___2_t, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadRender_Smooth_m5380759CCAE4BC8D1592EBE7088C4AE973E5E9BE (float ___0_a, float ___1_b, float ___2_x, const RuntimeMethod* method) ;
inline Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* List_1_get_Item_m658740103DB2DE2E2FF16DEFD463120C34BBE346 (List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* __this, int32_t ___0_index, const RuntimeMethod* method)
{
	return ((  Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* (*) (List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE*, int32_t, const RuntimeMethod*))List_1_get_Item_m33561245D64798C2AB07584C0EC4F240E4839A38_gshared)(__this, ___0_index, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_op_UnaryNegation_mC104632D789B94979F0AAE197F1A89BDC3773D14 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 PayloadRender_Surface_m0E4A5D0B544C846124A7422DED6E452C4E21D72D (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* ___0_p, Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 ___1_h, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* ___2_n, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_Mul_mF80A0EED832B6F3269918C7C10FA91E36C56F5B8 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadRender_Pow_mA707E79B5D2EC2D61714F837291FBD2BFA7AEEA0 (float ___0_x, float ___1_p, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D PayloadRender_Env_m179AA6C5AB885643D562AD08FEE47EBB2F85A21A (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_r, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D PayloadRender_Tone_mC05451DA4C9A7A040DA88C66AE8AEA98A9D7D66A (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_c, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadRender_Filmic_mCA6AD448A9DA9D3EEDCAFFFAD769D171A2B1B782 (float ___0_x, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double Math_Pow_mEAE651F0858203FBE12B72B6A53951BBD0FB5265 (double ___0_x, double ___1_y, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadRender_Noise_mA392E35C9ECF60BDD72D5CCB2921218AA8E0A970 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_p, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadRender_Stitch_m9496EAB56707B260EE63E06BE961539E8C301B97 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_lp, float ___1_pitch, float ___2_w, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 PayloadRender_M_m9400B16719658CD34FD3CFA82FA9E8C75FD60D94 (float ___0_r, float ___1_g, float ___2_b, float ___3_diffuse, float ___4_sr, float ___5_sg, float ___6_sb, float ___7_specK, float ___8_power, float ___9_env, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 PayloadRender_Cells_mE701F463E8E2334ECAA9C3A611CF03FA2C88042A (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* ___0_p, Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 ___1_h, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___2_lp, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_FaceUv_m80043B41EF1004805E20B0FB665CF214947D3310 (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* ___0_p, Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 ___1_h, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___2_lp, float* ___3_u, float* ___4_v, float* ___5_hu, float* ___6_hv, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadRender_Line_m585D11C93BA5AD4FEEC6323F2E1D6B9A5980B59F (float ___0_x, float ___1_pitch, float ___2_halfW, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadRender_Frac_mCB86369BB619EE9BE740A60F04823BB2377D9BBF (float ___0_x, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadRender_Hash_mAFB67513BCEE7055828B4324DEA16502DC155896 (int32_t ___0_x, int32_t ___1_y, float ___2_seed, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadRender_Hash_mC127B3E3881473AB770AFC6672497E9A6266C5A9 (int32_t ___0_x, int32_t ___1_y, int32_t ___2_z, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D Prim_ToLocal_m3C6745FB67593049B03ED7B23000B1D2CE8618B2 (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_v, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Prim_Intersect_m6BE49C58D6429F03E78403320F517788BD7D3D84 (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_o, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_d, float ___2_maxT, float* ___3_t, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* ___4_n, const RuntimeMethod* method) ;
inline int32_t List_1_get_Count_mA4B6A7483D74113D7978E2DA5D1DEE4F906ECC23_inline (List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* __this, const RuntimeMethod* method)
{
	return ((  int32_t (*) (List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE*, const RuntimeMethod*))List_1_get_Count_m4407E4C389F22B8CEC282C15D56516658746C383_gshared_inline)(__this, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool PayloadForms_KnownForm_mF1438848D2B6822053BEC97A6A0E610587144811 (String_t* ___0_id, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Monitor_Exit_m05B2CF037E2214B3208198C282490A2A475653FA (RuntimeObject* ___0_obj, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Monitor_Enter_m3CDB589DA1300B513D55FDCFB52B63E879794149 (RuntimeObject* ___0_obj, bool* ___1_lockTaken, const RuntimeMethod* method) ;
inline bool Dictionary_2_TryGetValue_m269D507F7E00AC2566CC52C93E3E3A084A03B5F6 (Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A* __this, String_t* ___0_key, List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE** ___1_value, const RuntimeMethod* method)
{
	return ((  bool (*) (Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A*, String_t*, List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE**, const RuntimeMethod*))Dictionary_2_TryGetValue_mD15380A4ED7CDEE99EA45881577D26BA9CE1B849_gshared)(__this, ___0_key, ___1_value, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder__ctor_m0F97E24000AE731D157CC3CE7CC858AE13978122 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Cubesat_m569CAEAD9D5873EC30CE7562845619F6683DDD56 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_BusSat_m4800BB30BDA2347220667C6CE5F3F617966FD7D2 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Weather_mC8ADB2608AE61D9261571176769AD0165EDA58E4 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Comms_m2CE72A55FEB0AF6B819EAB02732355BA6DD6862F (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Telescope_m9E7DE068E58F019E17C7B5EE9B77EEBF98C130DD (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Probe_m6FB885F57F94D43D6769B849495A52BBB1F34B17 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Cargo_m02A2F4319102F01B39154A358AC3E2A3DFD476C2 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Crew_m88C2F633AE6D651D7E02A5F3BA86B2491372A84D (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Dispenser_mB71E6312D962D8722CCC8D2AF8D6DAE85176648F (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Relay_mB44BDFC48EEAE2E7A17D5E6156A4997516EC1304 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Lander_m0FCDC40AF68DF2D14FE60B4452A077ED07BB9827 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_MassBox_m258187CB848705238D8FB067EC6EDB6B33979073 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) ;
inline void Dictionary_2_set_Item_mB2347972BD237E936BF2A0F5921FF0335524879C (Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A* __this, String_t* ___0_key, List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* ___1_value, const RuntimeMethod* method)
{
	((  void (*) (Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A*, String_t*, List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE*, const RuntimeMethod*))Dictionary_2_set_Item_m1A840355E8EDAECEA9D0C6F5E51B248FAA449CBD_gshared)(__this, ___0_key, ___1_value, method);
}
inline void HashSet_1__ctor_m15C5D34CE1AC392262EDDB77250F68CEF3522CF4 (HashSet_1_tA7CAED1C5D01DEEEA947992FCF58776FEDC59BAC* __this, const RuntimeMethod* method)
{
	((  void (*) (HashSet_1_tA7CAED1C5D01DEEEA947992FCF58776FEDC59BAC*, const RuntimeMethod*))HashSet_1__ctor_mD2BBE225041537B7240E453D3E14991EB2169E2C_gshared)(__this, method);
}
inline bool HashSet_1_Add_m4351A844FAB44D68332AEF82045AE45A6340E64C (HashSet_1_tA7CAED1C5D01DEEEA947992FCF58776FEDC59BAC* __this, int32_t ___0_item, const RuntimeMethod* method)
{
	return ((  bool (*) (HashSet_1_tA7CAED1C5D01DEEEA947992FCF58776FEDC59BAC*, int32_t, const RuntimeMethod*))HashSet_1_Add_m4F8BBB0C65C72E41F10A7F83E2963B71A631153C_gshared)(__this, ___0_item, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* __this, float ___0_x, float ___1_y, float ___2_z, float ___3_w, float ___4_h, float ___5_d, int32_t ___6_s, float ___7_yawDeg, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1 (float ___0_x, float ___1_y, float ___2_z, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_b, float ___2_r, int32_t ___3_s, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder_Ball_m2926A9ADD07DBA2D6A030098084DB1CB63778121 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_c, float ___1_r, int32_t ___2_s, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder_Dish_mCA47CAB74FD0464BEEF7DF5630E487EC9CAD4A91 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_vertex, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_facing, float ___2_aperture, float ___3_sphereR, int32_t ___4_s, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder_Wing_m4000246F5FF33DAB01DA6B33EB1C083BB206263C (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_root, float ___1_dir, int32_t ___2_panels, float ___3_pw, float ___4_ph, float ___5_gap, float ___6_tiltDeg, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder_Cone_mAC63A28D0AAFE35F676E5A01469B36833304726E (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, float ___1_ra, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___2_b, float ___3_rb, int32_t ___4_s, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder_Slab_mAA7D3ABE211E7F908653A28B993A9888D6E4C9BE (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_c, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_axis, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___2_upHint, float ___3_w, float ___4_h, float ___5_d, int32_t ___6_s, const RuntimeMethod* method) ;
inline void Dictionary_2__ctor_m432862A4D3846A8EC1E05271D1CA0346C288B93D (Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A* __this, const RuntimeMethod* method)
{
	((  void (*) (Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A*, const RuntimeMethod*))Dictionary_2__ctor_m5B32FBC624618211EB461D59CFBB10E987FD1329_gshared)(__this, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint8_t PayloadRender_Byte_m6D17F208B2A40D9E9DEDE0C4D5872C416E674974 (float ___0_v, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Job_get_Done_m16154739569DD8E47D867CA2FE93E82CCFAA9E22 (Job_t0B1165E35BF83E24991BB9F20783AADD55C95267* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float Prim_get_DishDepth_m66DFD50533D955CCA37F193067B3DD66EB606B93 (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void U3CCornersU3Ed__18__ctor_mE86F5A80976E4C0A18FB1F6BEB98314475C2B6F1 (U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* __this, int32_t ___0_U3CU3E1__state, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Prim_HitBox_mEFE6D788F8CB72B3493221260D3214B3ED6A9CBE (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_o, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_d, float ___2_maxT, float* ___3_t, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* ___4_n, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Prim_HitCone_m422F74A22825028C177F99763BAB4BD83AD33CDF (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_o, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_d, float ___2_r0, float ___3_r1, float ___4_h, float ___5_maxT, float* ___6_t, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* ___7_n, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Prim_HitSphere_m2AB861B39C6A2C12482F9EF6F60D177F7F1DC90F (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_o, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_d, float ___2_maxT, float* ___3_t, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* ___4_n, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Prim_HitDish_m77A557A1FBE93CEE0CE49326D2E12F5EC9A80E95 (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_o, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_d, float ___2_maxT, float* ___3_t, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* ___4_n, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t Environment_get_CurrentManagedThreadId_m66483AADCCC13272EBDCD94D31D2E52603C24BDF (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void NotSupportedException__ctor_m1398D0CDE19B36AA3DE9392879738C1EA2439CDF (NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* U3CCornersU3Ed__18_System_Collections_Generic_IEnumerableU3CKestrel_Core_Flight_PayloadRender_V3U3E_GetEnumerator_m5E6A1A1C95E40426B75378166F60E7E29D570EA4 (U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Prim_Bake_mAC6BE30FFA8DAC0FDE930665B72453859C755AEB (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* __this, int32_t ___0_index, const RuntimeMethod* method) ;
inline void List_1_Add_mE2377BD05FECB1E7EA1473B773D581201C95A728_inline (List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* __this, Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* ___0_item, const RuntimeMethod* method)
{
	((  void (*) (List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE*, Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6*, const RuntimeMethod*))List_1_Add_mEBCF994CC3814631017F46A387B1A192ED6C85C7_gshared_inline)(__this, ___0_item, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Prim__ctor_m4C75AA3E29F3B4340FC7142CFBA005FDAA8EA60B (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder_Add_mCED8C193B63F017AC5E35654B49F1A6F1E06A8CF (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* __this, Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* ___0_p, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder_Basis_mCC3825AC627F97D83F0DF37825C687E62556B6EF (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_w, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_upHint, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* ___2_u, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* ___3_v, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* ___4_ww, const RuntimeMethod* method) ;
inline void List_1__ctor_mCB861F16F94DDD8BB0B8397D76C21FEE49D42687 (List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* __this, const RuntimeMethod* method)
{
	((  void (*) (List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE*, const RuntimeMethod*))List_1__ctor_m7F078BB342729BDF11327FD89D7872265328F690_gshared)(__this, method);
}
inline void List_1_AddWithResize_m79A9BF770BEF9C06BE40D5401E55E375F2726CC4 (List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D* __this, RuntimeObject* ___0_item, const RuntimeMethod* method)
{
	((  void (*) (List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D*, RuntimeObject*, const RuntimeMethod*))List_1_AddWithResize_m79A9BF770BEF9C06BE40D5401E55E375F2726CC4_gshared)(__this, ___0_item, method);
}
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadPart__ctor_mCCEF62D79BA5DFB74637E8579AC4049EDC15D60B (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E* __this, float ___0_x, float ___1_y, float ___2_z, float ___3_w, float ___4_h, float ___5_d, uint8_t ___6_r, uint8_t ___7_g, uint8_t ___8_b, const RuntimeMethod* method) 
{
	{
		float L_0 = ___0_x;
		__this->___X = L_0;
		float L_1 = ___1_y;
		__this->___Y = L_1;
		float L_2 = ___2_z;
		__this->___Z = L_2;
		float L_3 = ___3_w;
		__this->___W = L_3;
		float L_4 = ___4_h;
		__this->___H = L_4;
		float L_5 = ___5_d;
		__this->___D = L_5;
		uint8_t L_6 = ___6_r;
		__this->___R = L_6;
		uint8_t L_7 = ___7_g;
		__this->___G = L_7;
		uint8_t L_8 = ___8_b;
		__this->___B = L_8;
		return;
	}
}
IL2CPP_EXTERN_C  void PayloadPart__ctor_mCCEF62D79BA5DFB74637E8579AC4049EDC15D60B_AdjustorThunk (RuntimeObject* __this, float ___0_x, float ___1_y, float ___2_z, float ___3_w, float ___4_h, float ___5_d, uint8_t ___6_r, uint8_t ___7_g, uint8_t ___8_b, const RuntimeMethod* method)
{
	PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E*>(__this + _offset);
	PayloadPart__ctor_mCCEF62D79BA5DFB74637E8579AC4049EDC15D60B(_thisAdjusted, ___0_x, ___1_y, ___2_z, ___3_w, ___4_h, ___5_d, ___6_r, ___7_g, ___8_b, method);
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* PayloadForms_get_Ids_mC28791119EB2FF88D4CDBDB1C881C351BDC5E17E (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_0 = ((PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_StaticFields*)il2cpp_codegen_static_fields_for(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var))->___Known;
		return (RuntimeObject*)L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool PayloadForms_KnownForm_mF1438848D2B6822053BEC97A6A0E610587144811 (String_t* ___0_id, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	{
		String_t* L_0 = ___0_id;
		bool L_1;
		L_1 = String_IsNullOrEmpty_mEA9E3FB005AC28FE02E69FCF95A7B8456192B478(L_0, NULL);
		if (!L_1)
		{
			goto IL_000a;
		}
	}
	{
		return (bool)0;
	}

IL_000a:
	{
		V_0 = 0;
		goto IL_0023;
	}

IL_000e:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_2 = ((PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_StaticFields*)il2cpp_codegen_static_fields_for(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var))->___Known;
		int32_t L_3 = V_0;
		NullCheck(L_2);
		int32_t L_4 = L_3;
		String_t* L_5 = (L_2)->GetAt(static_cast<il2cpp_array_size_t>(L_4));
		String_t* L_6 = ___0_id;
		bool L_7;
		L_7 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_5, L_6, NULL);
		if (!L_7)
		{
			goto IL_001f;
		}
	}
	{
		return (bool)1;
	}

IL_001f:
	{
		int32_t L_8 = V_0;
		V_0 = ((int32_t)il2cpp_codegen_add(L_8, 1));
	}

IL_0023:
	{
		int32_t L_9 = V_0;
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_10 = ((PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_StaticFields*)il2cpp_codegen_static_fields_for(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var))->___Known;
		NullCheck(L_10);
		if ((((int32_t)L_9) < ((int32_t)((int32_t)(((RuntimeArray*)L_10)->max_length)))))
		{
			goto IL_000e;
		}
	}
	{
		return (bool)0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Of_mC9B287472F9321C9F04A6C3D600C62ADF1E7EC19 (String_t* ___0_id, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral00990E38CA33B93B1431473E05022A9DCC677F93);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral1C43EC867F20B8DB8FA13AC94D6DB825367CF26C);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral3485BA793A227DB725EB436AF2FC1354566DEDF7);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral3D6DAD0DC8E1D6D4D37111226BD75DCE876A1C7E);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral4A3A8F16E29B08914BAD480CC20AF8B66E850DAB);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral4BF88E349C859F17645B0DD8922125ABAA3BAA05);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral52A70EE7ABEE0DD947480638E3B909A9A19C1D4E);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral9053C7BE1B5533FB96F3DE70C35EA0FFAB4FE19E);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralA9F5008AA90307FD7521614307E34133396662CA);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralBF8DB1FCA3176EF0EAAE60DA02213D8D74498AFE);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralD3CB15C4F0E233957BA9ED742AFB0EBF860C118F);
		s_Il2CppMethodInitialized = true;
	}
	uint32_t V_0 = 0;
	{
		String_t* L_0 = ___0_id;
		uint32_t L_1;
		L_1 = U3CPrivateImplementationDetailsU3E_ComputeStringHash_mC32B6AAD7FBB5563CF84923C7FCAA71C251E5DF0(L_0, NULL);
		V_0 = L_1;
		uint32_t L_2 = V_0;
		if ((!(((uint32_t)L_2) <= ((uint32_t)((int32_t)1607822841)))))
		{
			goto IL_0055;
		}
	}
	{
		uint32_t L_3 = V_0;
		if ((!(((uint32_t)L_3) <= ((uint32_t)((int32_t)380928450)))))
		{
			goto IL_0032;
		}
	}
	{
		uint32_t L_4 = V_0;
		if ((((int32_t)L_4) == ((int32_t)((int32_t)301197764))))
		{
			goto IL_00a3;
		}
	}
	{
		uint32_t L_5 = V_0;
		if ((((int32_t)L_5) == ((int32_t)((int32_t)380928450))))
		{
			goto IL_0148;
		}
	}
	{
		goto IL_01a8;
	}

IL_0032:
	{
		uint32_t L_6 = V_0;
		if ((((int32_t)L_6) == ((int32_t)((int32_t)910182291))))
		{
			goto IL_00cd;
		}
	}
	{
		uint32_t L_7 = V_0;
		if ((((int32_t)L_7) == ((int32_t)((int32_t)1231151528))))
		{
			goto IL_0139;
		}
	}
	{
		uint32_t L_8 = V_0;
		if ((((int32_t)L_8) == ((int32_t)((int32_t)1607822841))))
		{
			goto IL_00b8;
		}
	}
	{
		goto IL_01a8;
	}

IL_0055:
	{
		uint32_t L_9 = V_0;
		if ((!(((uint32_t)L_9) <= ((uint32_t)((int32_t)-1990644717)))))
		{
			goto IL_0080;
		}
	}
	{
		uint32_t L_10 = V_0;
		if ((((int32_t)L_10) == ((int32_t)((int32_t)2112007601))))
		{
			goto IL_0109;
		}
	}
	{
		uint32_t L_11 = V_0;
		if ((((int32_t)L_11) == ((int32_t)((int32_t)-2047630041))))
		{
			goto IL_011b;
		}
	}
	{
		uint32_t L_12 = V_0;
		if ((((int32_t)L_12) == ((int32_t)((int32_t)-1990644717))))
		{
			goto IL_00f7;
		}
	}
	{
		goto IL_01a8;
	}

IL_0080:
	{
		uint32_t L_13 = V_0;
		if ((((int32_t)L_13) == ((int32_t)((int32_t)-1823902856))))
		{
			goto IL_012a;
		}
	}
	{
		uint32_t L_14 = V_0;
		if ((((int32_t)L_14) == ((int32_t)((int32_t)-1523285333))))
		{
			goto IL_0157;
		}
	}
	{
		uint32_t L_15 = V_0;
		if ((((int32_t)L_15) == ((int32_t)((int32_t)-772103730))))
		{
			goto IL_00e2;
		}
	}
	{
		goto IL_01a8;
	}

IL_00a3:
	{
		String_t* L_16 = ___0_id;
		bool L_17;
		L_17 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_16, _stringLiteralBF8DB1FCA3176EF0EAAE60DA02213D8D74498AFE, NULL);
		if (L_17)
		{
			goto IL_0166;
		}
	}
	{
		goto IL_01a8;
	}

IL_00b8:
	{
		String_t* L_18 = ___0_id;
		bool L_19;
		L_19 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_18, _stringLiteral9053C7BE1B5533FB96F3DE70C35EA0FFAB4FE19E, NULL);
		if (L_19)
		{
			goto IL_016c;
		}
	}
	{
		goto IL_01a8;
	}

IL_00cd:
	{
		String_t* L_20 = ___0_id;
		bool L_21;
		L_21 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_20, _stringLiteralD3CB15C4F0E233957BA9ED742AFB0EBF860C118F, NULL);
		if (L_21)
		{
			goto IL_0172;
		}
	}
	{
		goto IL_01a8;
	}

IL_00e2:
	{
		String_t* L_22 = ___0_id;
		bool L_23;
		L_23 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_22, _stringLiteral1C43EC867F20B8DB8FA13AC94D6DB825367CF26C, NULL);
		if (L_23)
		{
			goto IL_0178;
		}
	}
	{
		goto IL_01a8;
	}

IL_00f7:
	{
		String_t* L_24 = ___0_id;
		bool L_25;
		L_25 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_24, _stringLiteral4BF88E349C859F17645B0DD8922125ABAA3BAA05, NULL);
		if (L_25)
		{
			goto IL_017e;
		}
	}
	{
		goto IL_01a8;
	}

IL_0109:
	{
		String_t* L_26 = ___0_id;
		bool L_27;
		L_27 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_26, _stringLiteral4A3A8F16E29B08914BAD480CC20AF8B66E850DAB, NULL);
		if (L_27)
		{
			goto IL_0184;
		}
	}
	{
		goto IL_01a8;
	}

IL_011b:
	{
		String_t* L_28 = ___0_id;
		bool L_29;
		L_29 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_28, _stringLiteral3485BA793A227DB725EB436AF2FC1354566DEDF7, NULL);
		if (L_29)
		{
			goto IL_018a;
		}
	}
	{
		goto IL_01a8;
	}

IL_012a:
	{
		String_t* L_30 = ___0_id;
		bool L_31;
		L_31 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_30, _stringLiteral00990E38CA33B93B1431473E05022A9DCC677F93, NULL);
		if (L_31)
		{
			goto IL_0190;
		}
	}
	{
		goto IL_01a8;
	}

IL_0139:
	{
		String_t* L_32 = ___0_id;
		bool L_33;
		L_33 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_32, _stringLiteral52A70EE7ABEE0DD947480638E3B909A9A19C1D4E, NULL);
		if (L_33)
		{
			goto IL_0196;
		}
	}
	{
		goto IL_01a8;
	}

IL_0148:
	{
		String_t* L_34 = ___0_id;
		bool L_35;
		L_35 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_34, _stringLiteralA9F5008AA90307FD7521614307E34133396662CA, NULL);
		if (L_35)
		{
			goto IL_019c;
		}
	}
	{
		goto IL_01a8;
	}

IL_0157:
	{
		String_t* L_36 = ___0_id;
		bool L_37;
		L_37 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_36, _stringLiteral3D6DAD0DC8E1D6D4D37111226BD75DCE876A1C7E, NULL);
		if (L_37)
		{
			goto IL_01a2;
		}
	}
	{
		goto IL_01a8;
	}

IL_0166:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_38;
		L_38 = PayloadForms_Cubesat_mD6939CF7FF6BB701A4D626A996D5623706C80C4A(NULL);
		return L_38;
	}

IL_016c:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_39;
		L_39 = PayloadForms_Bus_m018B3D5C17DBDE8D0F2D3F577D92D5550812322E(NULL);
		return L_39;
	}

IL_0172:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_40;
		L_40 = PayloadForms_Weather_m713ADEFFED4DABB3A7202A90D75DCC6332A01E05(NULL);
		return L_40;
	}

IL_0178:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_41;
		L_41 = PayloadForms_Comms_m9946E87C9DCB244BC2D9E6BA776D6A1AA7D33CA2(NULL);
		return L_41;
	}

IL_017e:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_42;
		L_42 = PayloadForms_Telescope_mE851270AD4FE36D4BF45AD4209F2952F89E6C8A7(NULL);
		return L_42;
	}

IL_0184:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_43;
		L_43 = PayloadForms_Probe_mC48A958CAC88EA1874BAF7A28B262321B9A715DC(NULL);
		return L_43;
	}

IL_018a:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_44;
		L_44 = PayloadForms_Cargo_m8C42096FDB79860910E259E4899B2F1641B5701A(NULL);
		return L_44;
	}

IL_0190:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_45;
		L_45 = PayloadForms_Crew_m4F7B1020ADA2118B5155504E8B0F9B5969BDF437(NULL);
		return L_45;
	}

IL_0196:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_46;
		L_46 = PayloadForms_Dispenser_m0824F74D9326461B9E6EDD6CA389B1C5CF0E9DBA(NULL);
		return L_46;
	}

IL_019c:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_47;
		L_47 = PayloadForms_Relay_m8610571C9500530B6CB4B612C8285A0D2AE63EA4(NULL);
		return L_47;
	}

IL_01a2:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_48;
		L_48 = PayloadForms_Lander_mE514D3383B00B13820AF571557D4C61F54E4D592(NULL);
		return L_48;
	}

IL_01a8:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_49;
		L_49 = PayloadForms_Box_mF168EBD81D889520193BADD0F403E968FABDC729(NULL);
		return L_49;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593 (float ___0_x, float ___1_y, float ___2_z, float ___3_w, float ___4_h, float ___5_d, uint8_t ___6_r, uint8_t ___7_g, uint8_t ___8_b, const RuntimeMethod* method) 
{
	{
		float L_0 = ___0_x;
		float L_1 = ___1_y;
		float L_2 = ___2_z;
		float L_3 = ___3_w;
		float L_4 = ___4_h;
		float L_5 = ___5_d;
		uint8_t L_6 = ___6_r;
		uint8_t L_7 = ___7_g;
		uint8_t L_8 = ___8_b;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_9;
		memset((&L_9), 0, sizeof(L_9));
		PayloadPart__ctor_mCCEF62D79BA5DFB74637E8579AC4049EDC15D60B((&L_9), L_0, L_1, L_2, L_3, L_4, L_5, L_6, L_7, L_8, NULL);
		return L_9;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Box_mF168EBD81D889520193BADD0F403E968FABDC729 (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_0 = (PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)SZArrayNew(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var, (uint32_t)4);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_1 = L_0;
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_2;
		L_2 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.150000006f), (0.0f), (0.920000017f), (0.620000005f), (0.720000029f), (uint8_t)((int32_t)236), (uint8_t)((int32_t)226), (uint8_t)((int32_t)204), NULL);
		NullCheck(L_1);
		(L_1)->SetAt(static_cast<il2cpp_array_size_t>(0), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_2);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_3 = L_1;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_4;
		L_4 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.280000001f), (0.0f), (0.939999998f), (0.119999997f), (0.74000001f), (uint8_t)((int32_t)214), (uint8_t)((int32_t)122), (uint8_t)((int32_t)48), NULL);
		NullCheck(L_3);
		(L_3)->SetAt(static_cast<il2cpp_array_size_t>(1), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_4);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_5 = L_3;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_6;
		L_6 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.579999983f), (0.0f), (0.0799999982f), (0.419999987f), (0.0799999982f), (uint8_t)((int32_t)168), (uint8_t)((int32_t)178), (uint8_t)((int32_t)190), NULL);
		NullCheck(L_5);
		(L_5)->SetAt(static_cast<il2cpp_array_size_t>(2), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_6);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_7 = L_5;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_8;
		L_8 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.819999993f), (0.0f), (0.219999999f), (0.0599999987f), (0.0599999987f), (uint8_t)((int32_t)214), (uint8_t)((int32_t)122), (uint8_t)((int32_t)48), NULL);
		NullCheck(L_7);
		(L_7)->SetAt(static_cast<il2cpp_array_size_t>(3), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_8);
		return L_7;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Cubesat_mD6939CF7FF6BB701A4D626A996D5623706C80C4A (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_0 = (PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)SZArrayNew(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var, (uint32_t)5);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_1 = L_0;
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_2;
		L_2 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.200000003f), (0.0f), (0.419999987f), (0.419999987f), (0.419999987f), (uint8_t)((int32_t)196), (uint8_t)((int32_t)154), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_1);
		(L_1)->SetAt(static_cast<il2cpp_array_size_t>(0), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_2);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_3 = L_1;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_4;
		L_4 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((-0.460000008f), (0.200000003f), (0.0f), (0.5f), (0.360000014f), (0.0799999982f), (uint8_t)((int32_t)30), (uint8_t)((int32_t)46), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_3);
		(L_3)->SetAt(static_cast<il2cpp_array_size_t>(1), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_4);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_5 = L_3;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_6;
		L_6 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.460000008f), (0.200000003f), (0.0f), (0.5f), (0.360000014f), (0.0799999982f), (uint8_t)((int32_t)30), (uint8_t)((int32_t)46), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_5);
		(L_5)->SetAt(static_cast<il2cpp_array_size_t>(2), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_6);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_7 = L_5;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_8;
		L_8 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.519999981f), (0.0f), (0.0500000007f), (0.360000014f), (0.0500000007f), (uint8_t)((int32_t)168), (uint8_t)((int32_t)178), (uint8_t)((int32_t)190), NULL);
		NullCheck(L_7);
		(L_7)->SetAt(static_cast<il2cpp_array_size_t>(3), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_8);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_9 = L_7;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_10;
		L_10 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.720000029f), (0.0f), (0.159999996f), (0.0399999991f), (0.0399999991f), (uint8_t)((int32_t)214), (uint8_t)((int32_t)122), (uint8_t)((int32_t)48), NULL);
		NullCheck(L_9);
		(L_9)->SetAt(static_cast<il2cpp_array_size_t>(4), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_10);
		return L_9;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Bus_m018B3D5C17DBDE8D0F2D3F577D92D5550812322E (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_0 = (PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)SZArrayNew(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var, (uint32_t)5);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_1 = L_0;
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_2;
		L_2 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.159999996f), (0.0f), (0.699999988f), (0.460000008f), (0.699999988f), (uint8_t)((int32_t)30), (uint8_t)((int32_t)46), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_1);
		(L_1)->SetAt(static_cast<il2cpp_array_size_t>(0), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_2);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_3 = L_1;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_4;
		L_4 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.419999987f), (0.0f), (0.5f), (0.0799999982f), (0.5f), (uint8_t)((int32_t)236), (uint8_t)((int32_t)226), (uint8_t)((int32_t)204), NULL);
		NullCheck(L_3);
		(L_3)->SetAt(static_cast<il2cpp_array_size_t>(1), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_4);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_5 = L_3;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_6;
		L_6 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((-0.850000024f), (0.159999996f), (0.0f), (0.899999976f), (0.0799999982f), (0.419999987f), (uint8_t)((int32_t)126), (uint8_t)((int32_t)186), (uint8_t)((int32_t)206), NULL);
		NullCheck(L_5);
		(L_5)->SetAt(static_cast<il2cpp_array_size_t>(2), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_6);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_7 = L_5;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_8;
		L_8 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.850000024f), (0.159999996f), (0.0f), (0.899999976f), (0.0799999982f), (0.419999987f), (uint8_t)((int32_t)126), (uint8_t)((int32_t)186), (uint8_t)((int32_t)206), NULL);
		NullCheck(L_7);
		(L_7)->SetAt(static_cast<il2cpp_array_size_t>(3), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_8);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_9 = L_7;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_10;
		L_10 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.479999989f), (0.100000001f), (0.0599999987f), (0.280000001f), (0.0599999987f), (uint8_t)((int32_t)168), (uint8_t)((int32_t)178), (uint8_t)((int32_t)190), NULL);
		NullCheck(L_9);
		(L_9)->SetAt(static_cast<il2cpp_array_size_t>(4), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_10);
		return L_9;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Weather_m713ADEFFED4DABB3A7202A90D75DCC6332A01E05 (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_0 = (PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)SZArrayNew(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var, (uint32_t)6);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_1 = L_0;
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_2;
		L_2 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.119999997f), (0.0f), (0.620000005f), (0.400000006f), (0.620000005f), (uint8_t)((int32_t)30), (uint8_t)((int32_t)46), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_1);
		(L_1)->SetAt(static_cast<il2cpp_array_size_t>(0), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_2);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_3 = L_1;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_4;
		L_4 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.400000006f), (0.0f), (0.360000014f), (0.280000001f), (0.360000014f), (uint8_t)((int32_t)236), (uint8_t)((int32_t)226), (uint8_t)((int32_t)204), NULL);
		NullCheck(L_3);
		(L_3)->SetAt(static_cast<il2cpp_array_size_t>(1), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_4);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_5 = L_3;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_6;
		L_6 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.620000005f), (0.0f), (0.5f), (0.100000001f), (0.5f), (uint8_t)((int32_t)214), (uint8_t)((int32_t)122), (uint8_t)((int32_t)48), NULL);
		NullCheck(L_5);
		(L_5)->SetAt(static_cast<il2cpp_array_size_t>(2), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_6);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_7 = L_5;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_8;
		L_8 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((-0.779999971f), (0.119999997f), (0.0f), (0.850000024f), (0.0599999987f), (0.379999995f), (uint8_t)((int32_t)126), (uint8_t)((int32_t)186), (uint8_t)((int32_t)206), NULL);
		NullCheck(L_7);
		(L_7)->SetAt(static_cast<il2cpp_array_size_t>(3), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_8);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_9 = L_7;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_10;
		L_10 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.779999971f), (0.119999997f), (0.0f), (0.850000024f), (0.0599999987f), (0.379999995f), (uint8_t)((int32_t)126), (uint8_t)((int32_t)186), (uint8_t)((int32_t)206), NULL);
		NullCheck(L_9);
		(L_9)->SetAt(static_cast<il2cpp_array_size_t>(4), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_10);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_11 = L_9;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_12;
		L_12 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.280000001f), (0.479999989f), (0.280000001f), (0.280000001f), (0.0799999982f), (uint8_t)((int32_t)196), (uint8_t)((int32_t)154), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_11);
		(L_11)->SetAt(static_cast<il2cpp_array_size_t>(5), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_12);
		return L_11;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Comms_m9946E87C9DCB244BC2D9E6BA776D6A1AA7D33CA2 (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_0 = (PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)SZArrayNew(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var, (uint32_t)5);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_1 = L_0;
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_2;
		L_2 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.100000001f), (-0.0500000007f), (0.550000012f), (0.400000006f), (0.550000012f), (uint8_t)((int32_t)30), (uint8_t)((int32_t)46), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_1);
		(L_1)->SetAt(static_cast<il2cpp_array_size_t>(0), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_2);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_3 = L_1;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_4;
		L_4 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((-1.04999995f), (0.100000001f), (-0.0500000007f), (1.14999998f), (0.0500000007f), (0.360000014f), (uint8_t)((int32_t)126), (uint8_t)((int32_t)186), (uint8_t)((int32_t)206), NULL);
		NullCheck(L_3);
		(L_3)->SetAt(static_cast<il2cpp_array_size_t>(1), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_4);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_5 = L_3;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_6;
		L_6 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((1.04999995f), (0.100000001f), (-0.0500000007f), (1.14999998f), (0.0500000007f), (0.360000014f), (uint8_t)((int32_t)126), (uint8_t)((int32_t)186), (uint8_t)((int32_t)206), NULL);
		NullCheck(L_5);
		(L_5)->SetAt(static_cast<il2cpp_array_size_t>(2), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_6);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_7 = L_5;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_8;
		L_8 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.219999999f), (0.550000012f), (0.620000005f), (0.620000005f), (0.0799999982f), (uint8_t)((int32_t)196), (uint8_t)((int32_t)154), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_7);
		(L_7)->SetAt(static_cast<il2cpp_array_size_t>(3), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_8);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_9 = L_7;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_10;
		L_10 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.219999999f), (0.620000005f), (0.159999996f), (0.159999996f), (0.0599999987f), (uint8_t)((int32_t)214), (uint8_t)((int32_t)122), (uint8_t)((int32_t)48), NULL);
		NullCheck(L_9);
		(L_9)->SetAt(static_cast<il2cpp_array_size_t>(4), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_10);
		return L_9;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Telescope_mE851270AD4FE36D4BF45AD4209F2952F89E6C8A7 (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_0 = (PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)SZArrayNew(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var, (uint32_t)6);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_1 = L_0;
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_2;
		L_2 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.159999996f), (0.0f), (0.379999995f), (0.379999995f), (1.35000002f), (uint8_t)((int32_t)236), (uint8_t)((int32_t)226), (uint8_t)((int32_t)204), NULL);
		NullCheck(L_1);
		(L_1)->SetAt(static_cast<il2cpp_array_size_t>(0), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_2);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_3 = L_1;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_4;
		L_4 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.159999996f), (0.779999971f), (0.620000005f), (0.620000005f), (0.159999996f), (uint8_t)((int32_t)30), (uint8_t)((int32_t)46), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_3);
		(L_3)->SetAt(static_cast<il2cpp_array_size_t>(1), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_4);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_5 = L_3;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_6;
		L_6 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.159999996f), (-0.720000029f), (0.280000001f), (0.280000001f), (0.200000003f), (uint8_t)((int32_t)168), (uint8_t)((int32_t)178), (uint8_t)((int32_t)190), NULL);
		NullCheck(L_5);
		(L_5)->SetAt(static_cast<il2cpp_array_size_t>(2), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_6);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_7 = L_5;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_8;
		L_8 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((-0.550000012f), (0.159999996f), (-0.150000006f), (0.550000012f), (0.0500000007f), (0.280000001f), (uint8_t)((int32_t)126), (uint8_t)((int32_t)186), (uint8_t)((int32_t)206), NULL);
		NullCheck(L_7);
		(L_7)->SetAt(static_cast<il2cpp_array_size_t>(3), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_8);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_9 = L_7;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_10;
		L_10 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.550000012f), (0.159999996f), (-0.150000006f), (0.550000012f), (0.0500000007f), (0.280000001f), (uint8_t)((int32_t)126), (uint8_t)((int32_t)186), (uint8_t)((int32_t)206), NULL);
		NullCheck(L_9);
		(L_9)->SetAt(static_cast<il2cpp_array_size_t>(4), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_10);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_11 = L_9;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_12;
		L_12 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.400000006f), (0.100000001f), (0.159999996f), (0.0799999982f), (0.300000012f), (uint8_t)((int32_t)214), (uint8_t)((int32_t)122), (uint8_t)((int32_t)48), NULL);
		NullCheck(L_11);
		(L_11)->SetAt(static_cast<il2cpp_array_size_t>(5), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_12);
		return L_11;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Probe_mC48A958CAC88EA1874BAF7A28B262321B9A715DC (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_0 = (PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)SZArrayNew(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var, (uint32_t)6);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_1 = L_0;
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_2;
		L_2 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.140000001f), (-0.100000001f), (0.5f), (0.360000014f), (0.5f), (uint8_t)((int32_t)30), (uint8_t)((int32_t)46), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_1);
		(L_1)->SetAt(static_cast<il2cpp_array_size_t>(0), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_2);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_3 = L_1;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_4;
		L_4 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.200000003f), (0.479999989f), (0.699999988f), (0.699999988f), (0.0799999982f), (uint8_t)((int32_t)196), (uint8_t)((int32_t)154), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_3);
		(L_3)->SetAt(static_cast<il2cpp_array_size_t>(1), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_4);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_5 = L_3;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_6;
		L_6 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.200000003f), (0.550000012f), (0.180000007f), (0.180000007f), (0.0599999987f), (uint8_t)((int32_t)214), (uint8_t)((int32_t)122), (uint8_t)((int32_t)48), NULL);
		NullCheck(L_5);
		(L_5)->SetAt(static_cast<il2cpp_array_size_t>(2), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_6);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_7 = L_5;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_8;
		L_8 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.419999987f), (0.140000001f), (-0.349999994f), (0.550000012f), (0.0799999982f), (0.0799999982f), (uint8_t)((int32_t)168), (uint8_t)((int32_t)178), (uint8_t)((int32_t)190), NULL);
		NullCheck(L_7);
		(L_7)->SetAt(static_cast<il2cpp_array_size_t>(3), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_8);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_9 = L_7;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_10;
		L_10 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.720000029f), (0.140000001f), (-0.349999994f), (0.159999996f), (0.159999996f), (0.159999996f), (uint8_t)((int32_t)236), (uint8_t)((int32_t)226), (uint8_t)((int32_t)204), NULL);
		NullCheck(L_9);
		(L_9)->SetAt(static_cast<il2cpp_array_size_t>(4), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_10);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_11 = L_9;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_12;
		L_12 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((-0.419999987f), (0.300000012f), (-0.100000001f), (0.280000001f), (0.0799999982f), (0.219999999f), (uint8_t)((int32_t)126), (uint8_t)((int32_t)186), (uint8_t)((int32_t)206), NULL);
		NullCheck(L_11);
		(L_11)->SetAt(static_cast<il2cpp_array_size_t>(5), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_12);
		return L_11;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Cargo_m8C42096FDB79860910E259E4899B2F1641B5701A (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_0 = (PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)SZArrayNew(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var, (uint32_t)5);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_1 = L_0;
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_2;
		L_2 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (-0.150000006f), (0.0f), (0.699999988f), (0.550000012f), (0.699999988f), (uint8_t)((int32_t)168), (uint8_t)((int32_t)178), (uint8_t)((int32_t)190), NULL);
		NullCheck(L_1);
		(L_1)->SetAt(static_cast<il2cpp_array_size_t>(0), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_2);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_3 = L_1;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_4;
		L_4 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.280000001f), (0.0f), (0.779999971f), (0.319999993f), (0.779999971f), (uint8_t)((int32_t)236), (uint8_t)((int32_t)226), (uint8_t)((int32_t)204), NULL);
		NullCheck(L_3);
		(L_3)->SetAt(static_cast<il2cpp_array_size_t>(1), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_4);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_5 = L_3;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_6;
		L_6 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.550000012f), (0.0f), (0.550000012f), (0.239999995f), (0.550000012f), (uint8_t)((int32_t)236), (uint8_t)((int32_t)226), (uint8_t)((int32_t)204), NULL);
		NullCheck(L_5);
		(L_5)->SetAt(static_cast<il2cpp_array_size_t>(2), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_6);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_7 = L_5;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_8;
		L_8 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.75999999f), (0.0f), (0.280000001f), (0.159999996f), (0.280000001f), (uint8_t)((int32_t)214), (uint8_t)((int32_t)122), (uint8_t)((int32_t)48), NULL);
		NullCheck(L_7);
		(L_7)->SetAt(static_cast<il2cpp_array_size_t>(3), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_8);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_9 = L_7;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_10;
		L_10 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.0500000007f), (0.400000006f), (0.219999999f), (0.159999996f), (0.0399999991f), (uint8_t)((int32_t)30), (uint8_t)((int32_t)46), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_9);
		(L_9)->SetAt(static_cast<il2cpp_array_size_t>(4), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_10);
		return L_9;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Crew_m4F7B1020ADA2118B5155504E8B0F9B5969BDF437 (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_0 = (PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)SZArrayNew(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var, (uint32_t)6);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_1 = L_0;
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_2;
		L_2 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (-0.0500000007f), (0.0f), (0.779999971f), (0.419999987f), (0.779999971f), (uint8_t)((int32_t)236), (uint8_t)((int32_t)226), (uint8_t)((int32_t)204), NULL);
		NullCheck(L_1);
		(L_1)->SetAt(static_cast<il2cpp_array_size_t>(0), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_2);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_3 = L_1;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_4;
		L_4 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.319999993f), (0.0f), (0.699999988f), (0.360000014f), (0.699999988f), (uint8_t)((int32_t)236), (uint8_t)((int32_t)226), (uint8_t)((int32_t)204), NULL);
		NullCheck(L_3);
		(L_3)->SetAt(static_cast<il2cpp_array_size_t>(1), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_4);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_5 = L_3;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_6;
		L_6 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.579999983f), (0.0f), (0.400000006f), (0.200000003f), (0.400000006f), (uint8_t)((int32_t)30), (uint8_t)((int32_t)46), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_5);
		(L_5)->SetAt(static_cast<il2cpp_array_size_t>(2), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_6);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_7 = L_5;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_8;
		L_8 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.819999993f), (0.0f), (0.0799999982f), (0.340000004f), (0.0799999982f), (uint8_t)((int32_t)168), (uint8_t)((int32_t)178), (uint8_t)((int32_t)190), NULL);
		NullCheck(L_7);
		(L_7)->SetAt(static_cast<il2cpp_array_size_t>(3), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_8);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_9 = L_7;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_10;
		L_10 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (1.01999998f), (0.0f), (0.219999999f), (0.0799999982f), (0.219999999f), (uint8_t)((int32_t)214), (uint8_t)((int32_t)122), (uint8_t)((int32_t)48), NULL);
		NullCheck(L_9);
		(L_9)->SetAt(static_cast<il2cpp_array_size_t>(4), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_10);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_11 = L_9;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_12;
		L_12 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.379999995f), (0.360000014f), (0.280000001f), (0.100000001f), (0.0399999991f), (uint8_t)((int32_t)126), (uint8_t)((int32_t)186), (uint8_t)((int32_t)206), NULL);
		NullCheck(L_11);
		(L_11)->SetAt(static_cast<il2cpp_array_size_t>(5), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_12);
		return L_11;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Dispenser_m0824F74D9326461B9E6EDD6CA389B1C5CF0E9DBA (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_0 = (PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)SZArrayNew(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var, (uint32_t)7);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_1 = L_0;
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_2;
		L_2 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.0799999982f), (0.0f), (0.460000008f), (0.280000001f), (1.45000005f), (uint8_t)((int32_t)30), (uint8_t)((int32_t)46), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_1);
		(L_1)->SetAt(static_cast<il2cpp_array_size_t>(0), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_2);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_3 = L_1;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_4;
		L_4 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.280000001f), (-0.479999989f), (0.280000001f), (0.219999999f), (0.280000001f), (uint8_t)((int32_t)196), (uint8_t)((int32_t)154), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_3);
		(L_3)->SetAt(static_cast<il2cpp_array_size_t>(1), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_4);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_5 = L_3;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_6;
		L_6 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.280000001f), (-0.159999996f), (0.280000001f), (0.219999999f), (0.280000001f), (uint8_t)((int32_t)196), (uint8_t)((int32_t)154), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_5);
		(L_5)->SetAt(static_cast<il2cpp_array_size_t>(2), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_6);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_7 = L_5;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_8;
		L_8 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.280000001f), (0.159999996f), (0.280000001f), (0.219999999f), (0.280000001f), (uint8_t)((int32_t)196), (uint8_t)((int32_t)154), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_7);
		(L_7)->SetAt(static_cast<il2cpp_array_size_t>(3), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_8);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_9 = L_7;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_10;
		L_10 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.280000001f), (0.479999989f), (0.280000001f), (0.219999999f), (0.280000001f), (uint8_t)((int32_t)196), (uint8_t)((int32_t)154), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_9);
		(L_9)->SetAt(static_cast<il2cpp_array_size_t>(4), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_10);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_11 = L_9;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_12;
		L_12 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((-0.319999993f), (0.0799999982f), (0.0f), (0.0799999982f), (0.0799999982f), (1.39999998f), (uint8_t)((int32_t)214), (uint8_t)((int32_t)122), (uint8_t)((int32_t)48), NULL);
		NullCheck(L_11);
		(L_11)->SetAt(static_cast<il2cpp_array_size_t>(5), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_12);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_13 = L_11;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_14;
		L_14 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.319999993f), (0.0799999982f), (0.0f), (0.0799999982f), (0.0799999982f), (1.39999998f), (uint8_t)((int32_t)214), (uint8_t)((int32_t)122), (uint8_t)((int32_t)48), NULL);
		NullCheck(L_13);
		(L_13)->SetAt(static_cast<il2cpp_array_size_t>(6), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_14);
		return L_13;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Relay_m8610571C9500530B6CB4B612C8285A0D2AE63EA4 (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_0 = (PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)SZArrayNew(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var, (uint32_t)6);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_1 = L_0;
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_2;
		L_2 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.0500000007f), (-0.150000006f), (0.479999989f), (0.360000014f), (0.479999989f), (uint8_t)((int32_t)30), (uint8_t)((int32_t)46), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_1);
		(L_1)->SetAt(static_cast<il2cpp_array_size_t>(0), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_2);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_3 = L_1;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_4;
		L_4 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((-1.14999998f), (0.0500000007f), (-0.150000006f), (1.29999995f), (0.0500000007f), (0.319999993f), (uint8_t)((int32_t)126), (uint8_t)((int32_t)186), (uint8_t)((int32_t)206), NULL);
		NullCheck(L_3);
		(L_3)->SetAt(static_cast<il2cpp_array_size_t>(1), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_4);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_5 = L_3;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_6;
		L_6 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((1.14999998f), (0.0500000007f), (-0.150000006f), (1.29999995f), (0.0500000007f), (0.319999993f), (uint8_t)((int32_t)126), (uint8_t)((int32_t)186), (uint8_t)((int32_t)206), NULL);
		NullCheck(L_5);
		(L_5)->SetAt(static_cast<il2cpp_array_size_t>(2), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_6);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_7 = L_5;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_8;
		L_8 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.180000007f), (0.620000005f), (1.04999995f), (1.04999995f), (0.0799999982f), (uint8_t)((int32_t)196), (uint8_t)((int32_t)154), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_7);
		(L_7)->SetAt(static_cast<il2cpp_array_size_t>(3), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_8);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_9 = L_7;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_10;
		L_10 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.180000007f), (0.699999988f), (0.219999999f), (0.219999999f), (0.0599999987f), (uint8_t)((int32_t)214), (uint8_t)((int32_t)122), (uint8_t)((int32_t)48), NULL);
		NullCheck(L_9);
		(L_9)->SetAt(static_cast<il2cpp_array_size_t>(4), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_10);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_11 = L_9;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_12;
		L_12 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.319999993f), (-0.150000006f), (0.159999996f), (0.119999997f), (0.159999996f), (uint8_t)((int32_t)236), (uint8_t)((int32_t)226), (uint8_t)((int32_t)204), NULL);
		NullCheck(L_11);
		(L_11)->SetAt(static_cast<il2cpp_array_size_t>(5), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_12);
		return L_11;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* PayloadForms_Lander_mE514D3383B00B13820AF571557D4C61F54E4D592 (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_0 = (PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F*)SZArrayNew(PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F_il2cpp_TypeInfo_var, (uint32_t)8);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_1 = L_0;
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_2;
		L_2 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.219999999f), (0.0f), (0.699999988f), (0.159999996f), (0.699999988f), (uint8_t)((int32_t)236), (uint8_t)((int32_t)226), (uint8_t)((int32_t)204), NULL);
		NullCheck(L_1);
		(L_1)->SetAt(static_cast<il2cpp_array_size_t>(0), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_2);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_3 = L_1;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_4;
		L_4 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.419999987f), (0.0f), (0.360000014f), (0.280000001f), (0.360000014f), (uint8_t)((int32_t)30), (uint8_t)((int32_t)46), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_3);
		(L_3)->SetAt(static_cast<il2cpp_array_size_t>(1), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_4);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_5 = L_3;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_6;
		L_6 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.0799999982f), (0.0f), (0.219999999f), (0.159999996f), (0.219999999f), (uint8_t)((int32_t)168), (uint8_t)((int32_t)178), (uint8_t)((int32_t)190), NULL);
		NullCheck(L_5);
		(L_5)->SetAt(static_cast<il2cpp_array_size_t>(2), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_6);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_7 = L_5;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_8;
		L_8 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((-0.479999989f), (0.0199999996f), (-0.479999989f), (0.0799999982f), (0.419999987f), (0.0799999982f), (uint8_t)((int32_t)214), (uint8_t)((int32_t)122), (uint8_t)((int32_t)48), NULL);
		NullCheck(L_7);
		(L_7)->SetAt(static_cast<il2cpp_array_size_t>(3), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_8);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_9 = L_7;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_10;
		L_10 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.479999989f), (0.0199999996f), (-0.479999989f), (0.0799999982f), (0.419999987f), (0.0799999982f), (uint8_t)((int32_t)214), (uint8_t)((int32_t)122), (uint8_t)((int32_t)48), NULL);
		NullCheck(L_9);
		(L_9)->SetAt(static_cast<il2cpp_array_size_t>(4), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_10);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_11 = L_9;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_12;
		L_12 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((-0.479999989f), (0.0199999996f), (0.479999989f), (0.0799999982f), (0.419999987f), (0.0799999982f), (uint8_t)((int32_t)214), (uint8_t)((int32_t)122), (uint8_t)((int32_t)48), NULL);
		NullCheck(L_11);
		(L_11)->SetAt(static_cast<il2cpp_array_size_t>(5), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_12);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_13 = L_11;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_14;
		L_14 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.479999989f), (0.0199999996f), (0.479999989f), (0.0799999982f), (0.419999987f), (0.0799999982f), (uint8_t)((int32_t)214), (uint8_t)((int32_t)122), (uint8_t)((int32_t)48), NULL);
		NullCheck(L_13);
		(L_13)->SetAt(static_cast<il2cpp_array_size_t>(6), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_14);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_15 = L_13;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_16;
		L_16 = PayloadForms_P_m4C14FA1F4CB1D823E615864B872C818620C65593((0.0f), (0.620000005f), (0.0f), (0.280000001f), (0.0799999982f), (0.280000001f), (uint8_t)((int32_t)196), (uint8_t)((int32_t)154), (uint8_t)((int32_t)72), NULL);
		NullCheck(L_15);
		(L_15)->SetAt(static_cast<il2cpp_array_size_t>(7), (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E)L_16);
		return L_15;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadForms__cctor_m57C94E8CCA79C680A67570DD5A5D27CB3859F7C5 (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral00990E38CA33B93B1431473E05022A9DCC677F93);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral1C43EC867F20B8DB8FA13AC94D6DB825367CF26C);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral3485BA793A227DB725EB436AF2FC1354566DEDF7);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral3C4F52AE03EA8FEF5DEAA4BF1DCF2792C970C821);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral3D6DAD0DC8E1D6D4D37111226BD75DCE876A1C7E);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral4A3A8F16E29B08914BAD480CC20AF8B66E850DAB);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral4BF88E349C859F17645B0DD8922125ABAA3BAA05);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral52A70EE7ABEE0DD947480638E3B909A9A19C1D4E);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral9053C7BE1B5533FB96F3DE70C35EA0FFAB4FE19E);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralA9F5008AA90307FD7521614307E34133396662CA);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralBF8DB1FCA3176EF0EAAE60DA02213D8D74498AFE);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralD3CB15C4F0E233957BA9ED742AFB0EBF860C118F);
		s_Il2CppMethodInitialized = true;
	}
	{
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_0 = (StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248*)(StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248*)SZArrayNew(StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248_il2cpp_TypeInfo_var, (uint32_t)((int32_t)12));
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_1 = L_0;
		NullCheck(L_1);
		(L_1)->SetAt(static_cast<il2cpp_array_size_t>(0), (String_t*)_stringLiteral3C4F52AE03EA8FEF5DEAA4BF1DCF2792C970C821);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_2 = L_1;
		NullCheck(L_2);
		(L_2)->SetAt(static_cast<il2cpp_array_size_t>(1), (String_t*)_stringLiteralBF8DB1FCA3176EF0EAAE60DA02213D8D74498AFE);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_3 = L_2;
		NullCheck(L_3);
		(L_3)->SetAt(static_cast<il2cpp_array_size_t>(2), (String_t*)_stringLiteral9053C7BE1B5533FB96F3DE70C35EA0FFAB4FE19E);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_4 = L_3;
		NullCheck(L_4);
		(L_4)->SetAt(static_cast<il2cpp_array_size_t>(3), (String_t*)_stringLiteralD3CB15C4F0E233957BA9ED742AFB0EBF860C118F);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_5 = L_4;
		NullCheck(L_5);
		(L_5)->SetAt(static_cast<il2cpp_array_size_t>(4), (String_t*)_stringLiteral1C43EC867F20B8DB8FA13AC94D6DB825367CF26C);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_6 = L_5;
		NullCheck(L_6);
		(L_6)->SetAt(static_cast<il2cpp_array_size_t>(5), (String_t*)_stringLiteral4BF88E349C859F17645B0DD8922125ABAA3BAA05);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_7 = L_6;
		NullCheck(L_7);
		(L_7)->SetAt(static_cast<il2cpp_array_size_t>(6), (String_t*)_stringLiteral4A3A8F16E29B08914BAD480CC20AF8B66E850DAB);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_8 = L_7;
		NullCheck(L_8);
		(L_8)->SetAt(static_cast<il2cpp_array_size_t>(7), (String_t*)_stringLiteral3485BA793A227DB725EB436AF2FC1354566DEDF7);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_9 = L_8;
		NullCheck(L_9);
		(L_9)->SetAt(static_cast<il2cpp_array_size_t>(8), (String_t*)_stringLiteral00990E38CA33B93B1431473E05022A9DCC677F93);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_10 = L_9;
		NullCheck(L_10);
		(L_10)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)9)), (String_t*)_stringLiteral52A70EE7ABEE0DD947480638E3B909A9A19C1D4E);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_11 = L_10;
		NullCheck(L_11);
		(L_11)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)10)), (String_t*)_stringLiteralA9F5008AA90307FD7521614307E34133396662CA);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_12 = L_11;
		NullCheck(L_12);
		(L_12)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)11)), (String_t*)_stringLiteral3D6DAD0DC8E1D6D4D37111226BD75DCE876A1C7E);
		((PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_StaticFields*)il2cpp_codegen_static_fields_for(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var))->___Known = L_12;
		Il2CppCodeGenWriteBarrier((void**)(&((PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_StaticFields*)il2cpp_codegen_static_fields_for(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var))->___Known), (void*)L_12);
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
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* PayloadPortrait_Rgba_mE697FE81F534A6E3DB3652B4B0FC265EED3D5B81 (String_t* ___0_form, int32_t ___1_size, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Array_Sort_TisFace_tDD2362557504D5249B24936FC555B3D54E56E6BE_m0D4A3A8826843727533FDA0D624989B397237008_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* V_0 = NULL;
	ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* V_1 = NULL;
	SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* V_2 = NULL;
	Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 V_3;
	memset((&V_3), 0, sizeof(V_3));
	Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 V_4;
	memset((&V_4), 0, sizeof(V_4));
	Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 V_5;
	memset((&V_5), 0, sizeof(V_5));
	Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 V_6;
	memset((&V_6), 0, sizeof(V_6));
	Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 V_7;
	memset((&V_7), 0, sizeof(V_7));
	int32_t V_8 = 0;
	VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* V_9 = NULL;
	float V_10 = 0.0f;
	float V_11 = 0.0f;
	float V_12 = 0.0f;
	float V_13 = 0.0f;
	float V_14 = 0.0f;
	float V_15 = 0.0f;
	float V_16 = 0.0f;
	float V_17 = 0.0f;
	FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539* V_18 = NULL;
	int32_t V_19 = 0;
	int32_t V_20 = 0;
	int32_t V_21 = 0;
	float V_22 = 0.0f;
	float V_23 = 0.0f;
	float V_24 = 0.0f;
	float V_25 = 0.0f;
	int32_t V_26 = 0;
	PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E V_27;
	memset((&V_27), 0, sizeof(V_27));
	int32_t V_28 = 0;
	int32_t V_29 = 0;
	Face_tDD2362557504D5249B24936FC555B3D54E56E6BE V_30;
	memset((&V_30), 0, sizeof(V_30));
	Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 V_31;
	memset((&V_31), 0, sizeof(V_31));
	Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 V_32;
	memset((&V_32), 0, sizeof(V_32));
	Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 V_33;
	memset((&V_33), 0, sizeof(V_33));
	Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 V_34;
	memset((&V_34), 0, sizeof(V_34));
	{
		int32_t L_0 = ___1_size;
		if ((((int32_t)L_0) >= ((int32_t)((int32_t)16))))
		{
			goto IL_0009;
		}
	}
	{
		___1_size = ((int32_t)16);
	}

IL_0009:
	{
		int32_t L_1 = ___1_size;
		if ((((int32_t)L_1) <= ((int32_t)((int32_t)512))))
		{
			goto IL_0018;
		}
	}
	{
		___1_size = ((int32_t)512);
	}

IL_0018:
	{
		String_t* L_2 = ___0_form;
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_3;
		L_3 = PayloadForms_Of_mC9B287472F9321C9F04A6C3D600C62ADF1E7EC19(L_2, NULL);
		V_0 = L_3;
		int32_t L_4 = ___1_size;
		int32_t L_5 = ___1_size;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_6 = (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031*)(ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031*)SZArrayNew(ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031_il2cpp_TypeInfo_var, (uint32_t)((int32_t)il2cpp_codegen_multiply(((int32_t)il2cpp_codegen_multiply(L_4, L_5)), 4)));
		V_1 = L_6;
		int32_t L_7 = ___1_size;
		int32_t L_8 = ___1_size;
		SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* L_9 = (SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C*)(SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C*)SZArrayNew(SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C_il2cpp_TypeInfo_var, (uint32_t)((int32_t)il2cpp_codegen_multiply(L_7, L_8)));
		V_2 = L_9;
		V_20 = 0;
		goto IL_0047;
	}

IL_0038:
	{
		SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* L_10 = V_2;
		int32_t L_11 = V_20;
		NullCheck(L_10);
		(L_10)->SetAt(static_cast<il2cpp_array_size_t>(L_11), (float)(-std::numeric_limits<float>::infinity()));
		int32_t L_12 = V_20;
		V_20 = ((int32_t)il2cpp_codegen_add(L_12, 1));
	}

IL_0047:
	{
		int32_t L_13 = V_20;
		SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* L_14 = V_2;
		NullCheck(L_14);
		if ((((int32_t)L_13) < ((int32_t)((int32_t)(((RuntimeArray*)L_14)->max_length)))))
		{
			goto IL_0038;
		}
	}
	{
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_15;
		memset((&L_15), 0, sizeof(L_15));
		Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC((&L_15), (0.819999993f), (0.479999989f), (0.720000029f), NULL);
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_16;
		L_16 = PayloadPortrait_Norm_m29C7521F65F744765AAD755AB48176D383F5C4DE(L_15, NULL);
		V_3 = L_16;
		Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC((&V_4), (0.0f), (1.0f), (0.0f), NULL);
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_17 = V_3;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_18 = V_4;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_19;
		L_19 = PayloadPortrait_Cross_mE37122B76B6C8A2998DB6F4AADC3B52E868C18FA(L_17, L_18, NULL);
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_20;
		L_20 = PayloadPortrait_Norm_m29C7521F65F744765AAD755AB48176D383F5C4DE(L_19, NULL);
		V_5 = L_20;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_21 = V_5;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_22 = V_3;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_23;
		L_23 = PayloadPortrait_Cross_mE37122B76B6C8A2998DB6F4AADC3B52E868C18FA(L_21, L_22, NULL);
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_24;
		L_24 = PayloadPortrait_Norm_m29C7521F65F744765AAD755AB48176D383F5C4DE(L_23, NULL);
		V_6 = L_24;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_25;
		memset((&L_25), 0, sizeof(L_25));
		Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC((&L_25), (0.319999993f), (0.899999976f), (0.280000001f), NULL);
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_26;
		L_26 = PayloadPortrait_Norm_m29C7521F65F744765AAD755AB48176D383F5C4DE(L_25, NULL);
		V_7 = L_26;
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_27 = V_0;
		NullCheck(L_27);
		V_8 = ((int32_t)(((RuntimeArray*)L_27)->max_length));
		int32_t L_28 = V_8;
		VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* L_29 = (VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00*)(VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00*)SZArrayNew(VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00_il2cpp_TypeInfo_var, (uint32_t)((int32_t)il2cpp_codegen_multiply(L_28, 8)));
		V_9 = L_29;
		V_10 = (std::numeric_limits<float>::infinity());
		V_11 = (std::numeric_limits<float>::infinity());
		V_12 = (-std::numeric_limits<float>::infinity());
		V_13 = (-std::numeric_limits<float>::infinity());
		V_21 = 0;
		goto IL_0136;
	}

IL_00e8:
	{
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_30 = V_0;
		int32_t L_31 = V_21;
		NullCheck(L_30);
		int32_t L_32 = L_31;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_33 = (L_30)->GetAt(static_cast<il2cpp_array_size_t>(L_32));
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_34 = V_3;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_35 = V_5;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_36 = V_6;
		VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* L_37 = V_9;
		int32_t L_38 = V_21;
		PayloadPortrait_Corners_m984E1DD5DB4B7C3BABC0A4D9F571ADF905597947(L_33, L_34, L_35, L_36, L_37, ((int32_t)il2cpp_codegen_multiply(L_38, 8)), (&V_22), (&V_23), (&V_24), (&V_25), NULL);
		float L_39 = V_22;
		float L_40 = V_10;
		if ((!(((float)L_39) < ((float)L_40))))
		{
			goto IL_0112;
		}
	}
	{
		float L_41 = V_22;
		V_10 = L_41;
	}

IL_0112:
	{
		float L_42 = V_23;
		float L_43 = V_11;
		if ((!(((float)L_42) < ((float)L_43))))
		{
			goto IL_011c;
		}
	}
	{
		float L_44 = V_23;
		V_11 = L_44;
	}

IL_011c:
	{
		float L_45 = V_24;
		float L_46 = V_12;
		if ((!(((float)L_45) > ((float)L_46))))
		{
			goto IL_0126;
		}
	}
	{
		float L_47 = V_24;
		V_12 = L_47;
	}

IL_0126:
	{
		float L_48 = V_25;
		float L_49 = V_13;
		if ((!(((float)L_48) > ((float)L_49))))
		{
			goto IL_0130;
		}
	}
	{
		float L_50 = V_25;
		V_13 = L_50;
	}

IL_0130:
	{
		int32_t L_51 = V_21;
		V_21 = ((int32_t)il2cpp_codegen_add(L_51, 1));
	}

IL_0136:
	{
		int32_t L_52 = V_21;
		int32_t L_53 = V_8;
		if ((((int32_t)L_52) < ((int32_t)L_53)))
		{
			goto IL_00e8;
		}
	}
	{
		float L_54 = V_12;
		float L_55 = V_10;
		float L_56 = V_13;
		float L_57 = V_11;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_58;
		L_58 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA(((float)il2cpp_codegen_subtract(L_54, L_55)), ((float)il2cpp_codegen_subtract(L_56, L_57)), NULL);
		V_14 = L_58;
		float L_59 = V_14;
		if ((((float)L_59) > ((float)(9.99999975E-05f))))
		{
			goto IL_015d;
		}
	}
	{
		V_14 = (1.0f);
	}

IL_015d:
	{
		int32_t L_60 = ___1_size;
		float L_61 = V_14;
		V_15 = ((float)(((float)il2cpp_codegen_multiply(((float)L_60), (0.779999971f)))/L_61));
		float L_62 = V_10;
		float L_63 = V_12;
		V_16 = ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_add(L_62, L_63)), (0.5f)));
		float L_64 = V_11;
		float L_65 = V_13;
		V_17 = ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_add(L_64, L_65)), (0.5f)));
		int32_t L_66 = V_8;
		FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539* L_67 = (FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539*)(FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539*)SZArrayNew(FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539_il2cpp_TypeInfo_var, (uint32_t)((int32_t)il2cpp_codegen_multiply(L_66, 6)));
		V_18 = L_67;
		V_19 = 0;
		V_26 = 0;
		goto IL_02a6;
	}

IL_019a:
	{
		PayloadPartU5BU5D_t0956AB7806DE86F7EA4CF758014D9BD828CE4A6F* L_68 = V_0;
		int32_t L_69 = V_26;
		NullCheck(L_68);
		int32_t L_70 = L_69;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_71 = (L_68)->GetAt(static_cast<il2cpp_array_size_t>(L_70));
		V_27 = L_71;
		int32_t L_72 = V_26;
		V_28 = ((int32_t)il2cpp_codegen_multiply(L_72, 8));
		FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539* L_73 = V_18;
		VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* L_74 = V_9;
		int32_t L_75 = V_28;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_76 = V_27;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_77 = V_7;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_78;
		memset((&L_78), 0, sizeof(L_78));
		Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC((&L_78), (0.0f), (1.0f), (0.0f), NULL);
		PayloadPortrait_AddFace_m868C5E06CDDD3189CE2766E38EF3B0F733EF56F6(L_73, (&V_19), L_74, L_75, 2, 3, 7, 6, L_76, L_77, L_78, NULL);
		FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539* L_79 = V_18;
		VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* L_80 = V_9;
		int32_t L_81 = V_28;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_82 = V_27;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_83 = V_7;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_84;
		memset((&L_84), 0, sizeof(L_84));
		Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC((&L_84), (0.0f), (-1.0f), (0.0f), NULL);
		PayloadPortrait_AddFace_m868C5E06CDDD3189CE2766E38EF3B0F733EF56F6(L_79, (&V_19), L_80, L_81, 0, 4, 5, 1, L_82, L_83, L_84, NULL);
		FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539* L_85 = V_18;
		VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* L_86 = V_9;
		int32_t L_87 = V_28;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_88 = V_27;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_89 = V_7;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_90;
		memset((&L_90), 0, sizeof(L_90));
		Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC((&L_90), (1.0f), (0.0f), (0.0f), NULL);
		PayloadPortrait_AddFace_m868C5E06CDDD3189CE2766E38EF3B0F733EF56F6(L_85, (&V_19), L_86, L_87, 1, 3, 7, 5, L_88, L_89, L_90, NULL);
		FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539* L_91 = V_18;
		VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* L_92 = V_9;
		int32_t L_93 = V_28;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_94 = V_27;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_95 = V_7;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_96;
		memset((&L_96), 0, sizeof(L_96));
		Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC((&L_96), (-1.0f), (0.0f), (0.0f), NULL);
		PayloadPortrait_AddFace_m868C5E06CDDD3189CE2766E38EF3B0F733EF56F6(L_91, (&V_19), L_92, L_93, 0, 2, 6, 4, L_94, L_95, L_96, NULL);
		FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539* L_97 = V_18;
		VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* L_98 = V_9;
		int32_t L_99 = V_28;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_100 = V_27;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_101 = V_7;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_102;
		memset((&L_102), 0, sizeof(L_102));
		Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC((&L_102), (0.0f), (0.0f), (1.0f), NULL);
		PayloadPortrait_AddFace_m868C5E06CDDD3189CE2766E38EF3B0F733EF56F6(L_97, (&V_19), L_98, L_99, 4, 5, 7, 6, L_100, L_101, L_102, NULL);
		FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539* L_103 = V_18;
		VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* L_104 = V_9;
		int32_t L_105 = V_28;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_106 = V_27;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_107 = V_7;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_108;
		memset((&L_108), 0, sizeof(L_108));
		Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC((&L_108), (0.0f), (0.0f), (-1.0f), NULL);
		PayloadPortrait_AddFace_m868C5E06CDDD3189CE2766E38EF3B0F733EF56F6(L_103, (&V_19), L_104, L_105, 0, 1, 3, 2, L_106, L_107, L_108, NULL);
		int32_t L_109 = V_26;
		V_26 = ((int32_t)il2cpp_codegen_add(L_109, 1));
	}

IL_02a6:
	{
		int32_t L_110 = V_26;
		int32_t L_111 = V_8;
		if ((((int32_t)L_110) < ((int32_t)L_111)))
		{
			goto IL_019a;
		}
	}
	{
		FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539* L_112 = V_18;
		int32_t L_113 = V_19;
		il2cpp_codegen_runtime_class_init_inline(FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752_il2cpp_TypeInfo_var);
		FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752* L_114 = ((FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752_StaticFields*)il2cpp_codegen_static_fields_for(FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752_il2cpp_TypeInfo_var))->___FarFirst;
		Array_Sort_TisFace_tDD2362557504D5249B24936FC555B3D54E56E6BE_m0D4A3A8826843727533FDA0D624989B397237008(L_112, 0, L_113, L_114, Array_Sort_TisFace_tDD2362557504D5249B24936FC555B3D54E56E6BE_m0D4A3A8826843727533FDA0D624989B397237008_RuntimeMethod_var);
		V_29 = 0;
		goto IL_03ac;
	}

IL_02c6:
	{
		FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539* L_115 = V_18;
		int32_t L_116 = V_29;
		NullCheck(L_115);
		int32_t L_117 = L_116;
		Face_tDD2362557504D5249B24936FC555B3D54E56E6BE L_118 = (L_115)->GetAt(static_cast<il2cpp_array_size_t>(L_117));
		V_30 = L_118;
		Face_tDD2362557504D5249B24936FC555B3D54E56E6BE L_119 = V_30;
		float L_120 = L_119.___Shade;
		if ((((float)L_120) < ((float)(0.0199999996f))))
		{
			goto IL_03a6;
		}
	}
	{
		VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* L_121 = V_9;
		Face_tDD2362557504D5249B24936FC555B3D54E56E6BE L_122 = V_30;
		int32_t L_123 = L_122.___A;
		NullCheck(L_121);
		int32_t L_124 = L_123;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_125 = (L_121)->GetAt(static_cast<il2cpp_array_size_t>(L_124));
		float L_126 = V_16;
		float L_127 = V_17;
		float L_128 = V_15;
		int32_t L_129 = ___1_size;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_130;
		L_130 = PayloadPortrait_Screen_m3A69D4B7C548A9DB2FFD5B88FD035AD5044CF3F5(L_125, L_126, L_127, L_128, L_129, NULL);
		V_31 = L_130;
		VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* L_131 = V_9;
		Face_tDD2362557504D5249B24936FC555B3D54E56E6BE L_132 = V_30;
		int32_t L_133 = L_132.___B;
		NullCheck(L_131);
		int32_t L_134 = L_133;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_135 = (L_131)->GetAt(static_cast<il2cpp_array_size_t>(L_134));
		float L_136 = V_16;
		float L_137 = V_17;
		float L_138 = V_15;
		int32_t L_139 = ___1_size;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_140;
		L_140 = PayloadPortrait_Screen_m3A69D4B7C548A9DB2FFD5B88FD035AD5044CF3F5(L_135, L_136, L_137, L_138, L_139, NULL);
		V_32 = L_140;
		VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* L_141 = V_9;
		Face_tDD2362557504D5249B24936FC555B3D54E56E6BE L_142 = V_30;
		int32_t L_143 = L_142.___C;
		NullCheck(L_141);
		int32_t L_144 = L_143;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_145 = (L_141)->GetAt(static_cast<il2cpp_array_size_t>(L_144));
		float L_146 = V_16;
		float L_147 = V_17;
		float L_148 = V_15;
		int32_t L_149 = ___1_size;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_150;
		L_150 = PayloadPortrait_Screen_m3A69D4B7C548A9DB2FFD5B88FD035AD5044CF3F5(L_145, L_146, L_147, L_148, L_149, NULL);
		V_33 = L_150;
		VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* L_151 = V_9;
		Face_tDD2362557504D5249B24936FC555B3D54E56E6BE L_152 = V_30;
		int32_t L_153 = L_152.___D;
		NullCheck(L_151);
		int32_t L_154 = L_153;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_155 = (L_151)->GetAt(static_cast<il2cpp_array_size_t>(L_154));
		float L_156 = V_16;
		float L_157 = V_17;
		float L_158 = V_15;
		int32_t L_159 = ___1_size;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_160;
		L_160 = PayloadPortrait_Screen_m3A69D4B7C548A9DB2FFD5B88FD035AD5044CF3F5(L_155, L_156, L_157, L_158, L_159, NULL);
		V_34 = L_160;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_161 = V_1;
		SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* L_162 = V_2;
		int32_t L_163 = ___1_size;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_164 = V_31;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_165 = V_32;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_166 = V_33;
		Face_tDD2362557504D5249B24936FC555B3D54E56E6BE L_167 = V_30;
		float L_168 = L_167.___Z;
		Face_tDD2362557504D5249B24936FC555B3D54E56E6BE L_169 = V_30;
		uint8_t L_170 = L_169.___Cr;
		Face_tDD2362557504D5249B24936FC555B3D54E56E6BE L_171 = V_30;
		uint8_t L_172 = L_171.___Cg;
		Face_tDD2362557504D5249B24936FC555B3D54E56E6BE L_173 = V_30;
		uint8_t L_174 = L_173.___Cb;
		PayloadPortrait_Tri_m7F91E4A7D1249290E59C4093A730EA62BB195A43(L_161, L_162, L_163, L_164, L_165, L_166, L_168, L_170, L_172, L_174, NULL);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_175 = V_1;
		SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* L_176 = V_2;
		int32_t L_177 = ___1_size;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_178 = V_31;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_179 = V_33;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_180 = V_34;
		Face_tDD2362557504D5249B24936FC555B3D54E56E6BE L_181 = V_30;
		float L_182 = L_181.___Z;
		Face_tDD2362557504D5249B24936FC555B3D54E56E6BE L_183 = V_30;
		uint8_t L_184 = L_183.___Cr;
		Face_tDD2362557504D5249B24936FC555B3D54E56E6BE L_185 = V_30;
		uint8_t L_186 = L_185.___Cg;
		Face_tDD2362557504D5249B24936FC555B3D54E56E6BE L_187 = V_30;
		uint8_t L_188 = L_187.___Cb;
		PayloadPortrait_Tri_m7F91E4A7D1249290E59C4093A730EA62BB195A43(L_175, L_176, L_177, L_178, L_179, L_180, L_182, L_184, L_186, L_188, NULL);
	}

IL_03a6:
	{
		int32_t L_189 = V_29;
		V_29 = ((int32_t)il2cpp_codegen_add(L_189, 1));
	}

IL_03ac:
	{
		int32_t L_190 = V_29;
		int32_t L_191 = V_19;
		if ((((int32_t)L_190) < ((int32_t)L_191)))
		{
			goto IL_02c6;
		}
	}
	{
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_192 = V_1;
		int32_t L_193 = ___1_size;
		PayloadPortrait_Ink_m83FD502D90533537A20F3C82A96124A00EBCBF60(L_192, L_193, NULL);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_194 = V_1;
		return L_194;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* PayloadPortrait_Png_m83C5F81BD2FDC2529150CAABC865E1F3651FD5BE (String_t* ___0_form, int32_t ___1_size, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = ___1_size;
		int32_t L_1 = ___1_size;
		String_t* L_2 = ___0_form;
		int32_t L_3 = ___1_size;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_4;
		L_4 = PayloadPortrait_Rgba_mE697FE81F534A6E3DB3652B4B0FC265EED3D5B81(L_2, L_3, NULL);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_5;
		L_5 = PayloadPortrait_EncodePng_m70BAD5C2F9C8A28CF61592C8CC058E3EFEF2DC08(L_0, L_1, L_4, NULL);
		return L_5;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* PayloadPortrait_EncodePng_m70BAD5C2F9C8A28CF61592C8CC058E3EFEF2DC08 (int32_t ___0_w, int32_t ___1_h, ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___2_rgba, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Array_Resize_TisByte_t94D9231AC217BE4D2E004C4CD32DF6D099EA41A3_mDEA5A67641D397AE82678232B46E377173ED4297_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&U3CPrivateImplementationDetailsU3E_t91B5382CE2346E184773DCFBD60B8EF7D20A968C____4C4B6A3BE1314AB86138BEF4314DDE022E600960D8689A2C8F8631802D20DAB6_FieldInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral43AC98067E1CFB50179D46B2DADEF5A10848F8BB);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral91A69CE9B7240FE9A06FD5FC7D58A6D3255669B7);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralE7B97EE372F4025D4538E7D1797CE1F0D6F0409C);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* V_1 = NULL;
	ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* V_2 = NULL;
	ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* V_3 = NULL;
	int32_t V_4 = 0;
	int32_t V_5 = 0;
	int32_t V_6 = 0;
	{
		int32_t L_0 = ___0_w;
		V_0 = ((int32_t)il2cpp_codegen_multiply(L_0, 4));
		int32_t L_1 = V_0;
		int32_t L_2 = ___1_h;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_3 = (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031*)(ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031*)SZArrayNew(ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031_il2cpp_TypeInfo_var, (uint32_t)((int32_t)il2cpp_codegen_multiply(((int32_t)il2cpp_codegen_add(L_1, 1)), L_2)));
		V_1 = L_3;
		V_5 = 0;
		goto IL_0037;
	}

IL_0014:
	{
		int32_t L_4 = V_5;
		int32_t L_5 = V_0;
		V_6 = ((int32_t)il2cpp_codegen_multiply(L_4, ((int32_t)il2cpp_codegen_add(L_5, 1))));
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_6 = V_1;
		int32_t L_7 = V_6;
		NullCheck(L_6);
		(L_6)->SetAt(static_cast<il2cpp_array_size_t>(L_7), (uint8_t)0);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_8 = ___2_rgba;
		int32_t L_9 = V_5;
		int32_t L_10 = V_0;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_11 = V_1;
		int32_t L_12 = V_6;
		int32_t L_13 = V_0;
		Buffer_BlockCopy_m2F7BC0C5BA97C500E3F87D5008718F797E02B358((RuntimeArray*)L_8, ((int32_t)il2cpp_codegen_multiply(L_9, L_10)), (RuntimeArray*)L_11, ((int32_t)il2cpp_codegen_add(L_12, 1)), L_13, NULL);
		int32_t L_14 = V_5;
		V_5 = ((int32_t)il2cpp_codegen_add(L_14, 1));
	}

IL_0037:
	{
		int32_t L_15 = V_5;
		int32_t L_16 = ___1_h;
		if ((((int32_t)L_15) < ((int32_t)L_16)))
		{
			goto IL_0014;
		}
	}
	{
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_17 = V_1;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_18;
		L_18 = PayloadPortrait_Zlib_mBDCD2A945DE849A569570D76237699D522FB2A28(L_17, NULL);
		V_2 = L_18;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_19 = V_2;
		NullCheck(L_19);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_20 = (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031*)(ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031*)SZArrayNew(ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031_il2cpp_TypeInfo_var, (uint32_t)((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_add(((int32_t)45), ((int32_t)(((RuntimeArray*)L_19)->max_length)))), ((int32_t)12))));
		V_3 = L_20;
		V_4 = 0;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_21 = (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031*)(ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031*)SZArrayNew(ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031_il2cpp_TypeInfo_var, (uint32_t)8);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_22 = L_21;
		RuntimeFieldHandle_t6E4C45B6D2EA12FC99185805A7E77527899B25C5 L_23 = { reinterpret_cast<intptr_t> (U3CPrivateImplementationDetailsU3E_t91B5382CE2346E184773DCFBD60B8EF7D20A968C____4C4B6A3BE1314AB86138BEF4314DDE022E600960D8689A2C8F8631802D20DAB6_FieldInfo_var) };
		RuntimeHelpers_InitializeArray_m751372AA3F24FBF6DA9B9D687CBFA2DE436CAB9B((RuntimeArray*)L_22, L_23, NULL);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_24 = V_3;
		Buffer_BlockCopy_m2F7BC0C5BA97C500E3F87D5008718F797E02B358((RuntimeArray*)L_22, 0, (RuntimeArray*)L_24, 0, 8, NULL);
		V_4 = 8;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_25 = V_3;
		int32_t L_26 = V_4;
		int32_t L_27 = ___0_w;
		int32_t L_28 = ___1_h;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_29;
		L_29 = PayloadPortrait_Ihdr_mA24744E56AEA81D70DBD8289B77A595EB1A07C16(L_27, L_28, NULL);
		int32_t L_30;
		L_30 = PayloadPortrait_Chunk_m41C41C7B18F101C865A33CEA318FB20276027F44(L_25, L_26, _stringLiteral43AC98067E1CFB50179D46B2DADEF5A10848F8BB, L_29, NULL);
		V_4 = L_30;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_31 = V_3;
		int32_t L_32 = V_4;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_33 = V_2;
		int32_t L_34;
		L_34 = PayloadPortrait_Chunk_m41C41C7B18F101C865A33CEA318FB20276027F44(L_31, L_32, _stringLiteral91A69CE9B7240FE9A06FD5FC7D58A6D3255669B7, L_33, NULL);
		V_4 = L_34;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_35 = V_3;
		int32_t L_36 = V_4;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_37 = (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031*)(ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031*)SZArrayNew(ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031_il2cpp_TypeInfo_var, (uint32_t)0);
		int32_t L_38;
		L_38 = PayloadPortrait_Chunk_m41C41C7B18F101C865A33CEA318FB20276027F44(L_35, L_36, _stringLiteralE7B97EE372F4025D4538E7D1797CE1F0D6F0409C, L_37, NULL);
		V_4 = L_38;
		int32_t L_39 = V_4;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_40 = V_3;
		NullCheck(L_40);
		if ((((int32_t)L_39) == ((int32_t)((int32_t)(((RuntimeArray*)L_40)->max_length)))))
		{
			goto IL_00bd;
		}
	}
	{
		int32_t L_41 = V_4;
		Array_Resize_TisByte_t94D9231AC217BE4D2E004C4CD32DF6D099EA41A3_mDEA5A67641D397AE82678232B46E377173ED4297((&V_3), L_41, Array_Resize_TisByte_t94D9231AC217BE4D2E004C4CD32DF6D099EA41A3_mDEA5A67641D397AE82678232B46E377173ED4297_RuntimeMethod_var);
	}

IL_00bd:
	{
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_42 = V_3;
		return L_42;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadPortrait_Corners_m984E1DD5DB4B7C3BABC0A4D9F571ADF905597947 (PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E ___0_p, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___1_eye, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___2_right, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___3_up, VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* ___4_proj, int32_t ___5_at, float* ___6_minX, float* ___7_minY, float* ___8_maxX, float* ___9_maxY, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	float V_1 = 0.0f;
	float V_2 = 0.0f;
	SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* V_3 = NULL;
	int32_t V_4 = 0;
	int32_t V_5 = 0;
	int32_t V_6 = 0;
	int32_t V_7 = 0;
	float V_8 = 0.0f;
	float V_9 = 0.0f;
	float V_10 = 0.0f;
	{
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_0 = ___0_p;
		float L_1 = L_0.___W;
		V_0 = ((float)il2cpp_codegen_multiply(L_1, (0.5f)));
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_2 = ___0_p;
		float L_3 = L_2.___H;
		V_1 = ((float)il2cpp_codegen_multiply(L_3, (0.5f)));
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_4 = ___0_p;
		float L_5 = L_4.___D;
		V_2 = ((float)il2cpp_codegen_multiply(L_5, (0.5f)));
		SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* L_6 = (SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C*)(SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C*)SZArrayNew(SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C_il2cpp_TypeInfo_var, (uint32_t)2);
		SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* L_7 = L_6;
		NullCheck(L_7);
		(L_7)->SetAt(static_cast<il2cpp_array_size_t>(0), (float)(-1.0f));
		SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* L_8 = L_7;
		NullCheck(L_8);
		(L_8)->SetAt(static_cast<il2cpp_array_size_t>(1), (float)(1.0f));
		V_3 = L_8;
		float* L_9 = ___6_minX;
		*((float*)L_9) = (float)(std::numeric_limits<float>::infinity());
		float* L_10 = ___7_minY;
		float* L_11 = ___6_minX;
		float L_12 = *((float*)L_11);
		*((float*)L_10) = (float)L_12;
		float* L_13 = ___8_maxX;
		*((float*)L_13) = (float)(-std::numeric_limits<float>::infinity());
		float* L_14 = ___9_maxY;
		float* L_15 = ___8_maxX;
		float L_16 = *((float*)L_15);
		*((float*)L_14) = (float)L_16;
		V_4 = 0;
		V_5 = 0;
		goto IL_012a;
	}

IL_0065:
	{
		V_6 = 0;
		goto IL_011c;
	}

IL_006d:
	{
		V_7 = 0;
		goto IL_010e;
	}

IL_0075:
	{
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_17 = ___0_p;
		float L_18 = L_17.___X;
		SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* L_19 = V_3;
		int32_t L_20 = V_7;
		NullCheck(L_19);
		int32_t L_21 = L_20;
		float L_22 = (L_19)->GetAt(static_cast<il2cpp_array_size_t>(L_21));
		float L_23 = V_0;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_24 = ___0_p;
		float L_25 = L_24.___Y;
		SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* L_26 = V_3;
		int32_t L_27 = V_6;
		NullCheck(L_26);
		int32_t L_28 = L_27;
		float L_29 = (L_26)->GetAt(static_cast<il2cpp_array_size_t>(L_28));
		float L_30 = V_1;
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_31 = ___0_p;
		float L_32 = L_31.___Z;
		SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* L_33 = V_3;
		int32_t L_34 = V_5;
		NullCheck(L_33);
		int32_t L_35 = L_34;
		float L_36 = (L_33)->GetAt(static_cast<il2cpp_array_size_t>(L_35));
		float L_37 = V_2;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_38;
		memset((&L_38), 0, sizeof(L_38));
		Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC((&L_38), ((float)il2cpp_codegen_add(L_18, ((float)il2cpp_codegen_multiply(L_22, L_23)))), ((float)il2cpp_codegen_add(L_25, ((float)il2cpp_codegen_multiply(L_29, L_30)))), ((float)il2cpp_codegen_add(L_32, ((float)il2cpp_codegen_multiply(L_36, L_37)))), NULL);
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_39 = L_38;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_40 = ___2_right;
		float L_41;
		L_41 = PayloadPortrait_Dot_m3B2C1EFC28B630AD386621635E287771DB77CA7E(L_39, L_40, NULL);
		V_8 = L_41;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_42 = L_39;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_43 = ___3_up;
		float L_44;
		L_44 = PayloadPortrait_Dot_m3B2C1EFC28B630AD386621635E287771DB77CA7E(L_42, L_43, NULL);
		V_9 = L_44;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_45 = ___1_eye;
		float L_46;
		L_46 = PayloadPortrait_Dot_m3B2C1EFC28B630AD386621635E287771DB77CA7E(L_42, L_45, NULL);
		V_10 = L_46;
		VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* L_47 = ___4_proj;
		int32_t L_48 = ___5_at;
		int32_t L_49 = V_4;
		float L_50 = V_8;
		float L_51 = V_9;
		float L_52 = V_10;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_53;
		memset((&L_53), 0, sizeof(L_53));
		Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC((&L_53), L_50, L_51, L_52, NULL);
		NullCheck(L_47);
		(L_47)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_48, L_49))), (Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109)L_53);
		float L_54 = V_8;
		float* L_55 = ___6_minX;
		float L_56 = *((float*)L_55);
		if ((!(((float)L_54) < ((float)L_56))))
		{
			goto IL_00de;
		}
	}
	{
		float* L_57 = ___6_minX;
		float L_58 = V_8;
		*((float*)L_57) = (float)L_58;
	}

IL_00de:
	{
		float L_59 = V_9;
		float* L_60 = ___7_minY;
		float L_61 = *((float*)L_60);
		if ((!(((float)L_59) < ((float)L_61))))
		{
			goto IL_00ea;
		}
	}
	{
		float* L_62 = ___7_minY;
		float L_63 = V_9;
		*((float*)L_62) = (float)L_63;
	}

IL_00ea:
	{
		float L_64 = V_8;
		float* L_65 = ___8_maxX;
		float L_66 = *((float*)L_65);
		if ((!(((float)L_64) > ((float)L_66))))
		{
			goto IL_00f6;
		}
	}
	{
		float* L_67 = ___8_maxX;
		float L_68 = V_8;
		*((float*)L_67) = (float)L_68;
	}

IL_00f6:
	{
		float L_69 = V_9;
		float* L_70 = ___9_maxY;
		float L_71 = *((float*)L_70);
		if ((!(((float)L_69) > ((float)L_71))))
		{
			goto IL_0102;
		}
	}
	{
		float* L_72 = ___9_maxY;
		float L_73 = V_9;
		*((float*)L_72) = (float)L_73;
	}

IL_0102:
	{
		int32_t L_74 = V_4;
		V_4 = ((int32_t)il2cpp_codegen_add(L_74, 1));
		int32_t L_75 = V_7;
		V_7 = ((int32_t)il2cpp_codegen_add(L_75, 1));
	}

IL_010e:
	{
		int32_t L_76 = V_7;
		if ((((int32_t)L_76) < ((int32_t)2)))
		{
			goto IL_0075;
		}
	}
	{
		int32_t L_77 = V_6;
		V_6 = ((int32_t)il2cpp_codegen_add(L_77, 1));
	}

IL_011c:
	{
		int32_t L_78 = V_6;
		if ((((int32_t)L_78) < ((int32_t)2)))
		{
			goto IL_006d;
		}
	}
	{
		int32_t L_79 = V_5;
		V_5 = ((int32_t)il2cpp_codegen_add(L_79, 1));
	}

IL_012a:
	{
		int32_t L_80 = V_5;
		if ((((int32_t)L_80) < ((int32_t)2)))
		{
			goto IL_0065;
		}
	}
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadPortrait_AddFace_m868C5E06CDDD3189CE2766E38EF3B0F733EF56F6 (FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539* ___0_faces, int32_t* ___1_n, VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* ___2_proj, int32_t ___3_b, int32_t ___4_a, int32_t ___5_c, int32_t ___6_d, int32_t ___7_e, PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E ___8_part, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___9_light, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___10_normal, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	uint8_t V_1 = 0x0;
	uint8_t V_2 = 0x0;
	uint8_t V_3 = 0x0;
	float V_4 = 0.0f;
	int32_t V_5 = 0;
	Face_tDD2362557504D5249B24936FC555B3D54E56E6BE V_6;
	memset((&V_6), 0, sizeof(V_6));
	{
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_0 = ___10_normal;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_1 = ___9_light;
		float L_2;
		L_2 = PayloadPortrait_Dot_m3B2C1EFC28B630AD386621635E287771DB77CA7E(L_0, L_1, NULL);
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_3;
		L_3 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA((0.0f), L_2, NULL);
		V_0 = ((float)il2cpp_codegen_add((0.219999999f), ((float)il2cpp_codegen_multiply((0.779999971f), L_3))));
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_4 = ___8_part;
		uint8_t L_5 = L_4.___R;
		float L_6 = V_0;
		float L_7;
		L_7 = Math_Min_mE913811A2F7566294BF4649A434282634E7254B3((255.0f), ((float)il2cpp_codegen_multiply(((float)L_5), L_6)), NULL);
		V_1 = (uint8_t)il2cpp_codegen_cast_floating_point<uint8_t, int32_t, float>(L_7);
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_8 = ___8_part;
		uint8_t L_9 = L_8.___G;
		float L_10 = V_0;
		float L_11;
		L_11 = Math_Min_mE913811A2F7566294BF4649A434282634E7254B3((255.0f), ((float)il2cpp_codegen_multiply(((float)L_9), L_10)), NULL);
		V_2 = (uint8_t)il2cpp_codegen_cast_floating_point<uint8_t, int32_t, float>(L_11);
		PayloadPart_t577184D4B904D839530CFDBD57182B81824C301E L_12 = ___8_part;
		uint8_t L_13 = L_12.___B;
		float L_14 = V_0;
		float L_15;
		L_15 = Math_Min_mE913811A2F7566294BF4649A434282634E7254B3((255.0f), ((float)il2cpp_codegen_multiply(((float)L_13), L_14)), NULL);
		V_3 = (uint8_t)il2cpp_codegen_cast_floating_point<uint8_t, int32_t, float>(L_15);
		VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* L_16 = ___2_proj;
		int32_t L_17 = ___3_b;
		int32_t L_18 = ___4_a;
		NullCheck(L_16);
		float L_19 = ((L_16)->GetAddressAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_17, L_18)))))->___Z;
		VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* L_20 = ___2_proj;
		int32_t L_21 = ___3_b;
		int32_t L_22 = ___5_c;
		NullCheck(L_20);
		float L_23 = ((L_20)->GetAddressAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_21, L_22)))))->___Z;
		VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* L_24 = ___2_proj;
		int32_t L_25 = ___3_b;
		int32_t L_26 = ___6_d;
		NullCheck(L_24);
		float L_27 = ((L_24)->GetAddressAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_25, L_26)))))->___Z;
		VecU5BU5D_t096502E8F04C9FACFB7D50112B04E89DD7ED0D00* L_28 = ___2_proj;
		int32_t L_29 = ___3_b;
		int32_t L_30 = ___7_e;
		NullCheck(L_28);
		float L_31 = ((L_28)->GetAddressAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_29, L_30)))))->___Z;
		V_4 = ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_add(((float)il2cpp_codegen_add(((float)il2cpp_codegen_add(L_19, L_23)), L_27)), L_31)), (0.25f)));
		FaceU5BU5D_tBDD9E9042538D8A7988DD0FD31DF096BB352C539* L_32 = ___0_faces;
		int32_t* L_33 = ___1_n;
		int32_t* L_34 = ___1_n;
		int32_t L_35 = *((int32_t*)L_34);
		V_5 = L_35;
		int32_t L_36 = V_5;
		*((int32_t*)L_33) = (int32_t)((int32_t)il2cpp_codegen_add(L_36, 1));
		int32_t L_37 = V_5;
		il2cpp_codegen_initobj((&V_6), sizeof(Face_tDD2362557504D5249B24936FC555B3D54E56E6BE));
		int32_t L_38 = ___3_b;
		int32_t L_39 = ___4_a;
		(&V_6)->___A = ((int32_t)il2cpp_codegen_add(L_38, L_39));
		int32_t L_40 = ___3_b;
		int32_t L_41 = ___5_c;
		(&V_6)->___B = ((int32_t)il2cpp_codegen_add(L_40, L_41));
		int32_t L_42 = ___3_b;
		int32_t L_43 = ___6_d;
		(&V_6)->___C = ((int32_t)il2cpp_codegen_add(L_42, L_43));
		int32_t L_44 = ___3_b;
		int32_t L_45 = ___7_e;
		(&V_6)->___D = ((int32_t)il2cpp_codegen_add(L_44, L_45));
		float L_46 = V_4;
		(&V_6)->___Z = L_46;
		float L_47 = V_0;
		(&V_6)->___Shade = L_47;
		uint8_t L_48 = V_1;
		(&V_6)->___Cr = L_48;
		uint8_t L_49 = V_2;
		(&V_6)->___Cg = L_49;
		uint8_t L_50 = V_3;
		(&V_6)->___Cb = L_50;
		Face_tDD2362557504D5249B24936FC555B3D54E56E6BE L_51 = V_6;
		NullCheck(L_32);
		(L_32)->SetAt(static_cast<il2cpp_array_size_t>(L_37), (Face_tDD2362557504D5249B24936FC555B3D54E56E6BE)L_51);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 PayloadPortrait_Screen_m3A69D4B7C548A9DB2FFD5B88FD035AD5044CF3F5 (Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___0_p, float ___1_cx, float ___2_cy, float ___3_fit, int32_t ___4_size, const RuntimeMethod* method) 
{
	float V_0 = 0.0f;
	{
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_0 = ___0_p;
		float L_1 = L_0.___X;
		float L_2 = ___1_cx;
		float L_3 = ___3_fit;
		int32_t L_4 = ___4_size;
		float L_5 = ___2_cy;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_6 = ___0_p;
		float L_7 = L_6.___Y;
		float L_8 = ___3_fit;
		int32_t L_9 = ___4_size;
		V_0 = ((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_subtract(L_5, L_7)), L_8)), ((float)il2cpp_codegen_multiply(((float)L_9), (0.5f)))));
		float L_10 = V_0;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_11 = ___0_p;
		float L_12 = L_11.___Z;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_13;
		memset((&L_13), 0, sizeof(L_13));
		Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC((&L_13), ((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_subtract(L_1, L_2)), L_3)), ((float)il2cpp_codegen_multiply(((float)L_4), (0.5f))))), L_10, L_12, NULL);
		return L_13;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadPortrait_Tri_m7F91E4A7D1249290E59C4093A730EA62BB195A43 (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___0_pix, SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* ___1_depth, int32_t ___2_size, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___3_a, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___4_b, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___5_c, float ___6_z, uint8_t ___7_r, uint8_t ___8_g, uint8_t ___9_bl, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	int32_t V_1 = 0;
	int32_t V_2 = 0;
	int32_t V_3 = 0;
	float V_4 = 0.0f;
	int32_t V_5 = 0;
	int32_t V_6 = 0;
	Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 V_7;
	memset((&V_7), 0, sizeof(V_7));
	float V_8 = 0.0f;
	float V_9 = 0.0f;
	int32_t V_10 = 0;
	int32_t V_11 = 0;
	{
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_0 = ___3_a;
		float L_1 = L_0.___X;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_2 = ___4_b;
		float L_3 = L_2.___X;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_4 = ___5_c;
		float L_5 = L_4.___X;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_6;
		L_6 = Math_Min_mE913811A2F7566294BF4649A434282634E7254B3(L_3, L_5, NULL);
		float L_7;
		L_7 = Math_Min_mE913811A2F7566294BF4649A434282634E7254B3(L_1, L_6, NULL);
		double L_8;
		L_8 = floor(((double)L_7));
		V_0 = il2cpp_codegen_cast_double_to_int<int32_t>(L_8);
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_9 = ___3_a;
		float L_10 = L_9.___Y;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_11 = ___4_b;
		float L_12 = L_11.___Y;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_13 = ___5_c;
		float L_14 = L_13.___Y;
		float L_15;
		L_15 = Math_Min_mE913811A2F7566294BF4649A434282634E7254B3(L_12, L_14, NULL);
		float L_16;
		L_16 = Math_Min_mE913811A2F7566294BF4649A434282634E7254B3(L_10, L_15, NULL);
		double L_17;
		L_17 = floor(((double)L_16));
		V_1 = il2cpp_codegen_cast_double_to_int<int32_t>(L_17);
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_18 = ___3_a;
		float L_19 = L_18.___X;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_20 = ___4_b;
		float L_21 = L_20.___X;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_22 = ___5_c;
		float L_23 = L_22.___X;
		float L_24;
		L_24 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA(L_21, L_23, NULL);
		float L_25;
		L_25 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA(L_19, L_24, NULL);
		double L_26;
		L_26 = ceil(((double)L_25));
		V_2 = il2cpp_codegen_cast_double_to_int<int32_t>(L_26);
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_27 = ___3_a;
		float L_28 = L_27.___Y;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_29 = ___4_b;
		float L_30 = L_29.___Y;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_31 = ___5_c;
		float L_32 = L_31.___Y;
		float L_33;
		L_33 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA(L_30, L_32, NULL);
		float L_34;
		L_34 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA(L_28, L_33, NULL);
		double L_35;
		L_35 = ceil(((double)L_34));
		V_3 = il2cpp_codegen_cast_double_to_int<int32_t>(L_35);
		int32_t L_36 = V_0;
		if ((((int32_t)L_36) >= ((int32_t)0)))
		{
			goto IL_009e;
		}
	}
	{
		V_0 = 0;
	}

IL_009e:
	{
		int32_t L_37 = V_1;
		if ((((int32_t)L_37) >= ((int32_t)0)))
		{
			goto IL_00a4;
		}
	}
	{
		V_1 = 0;
	}

IL_00a4:
	{
		int32_t L_38 = V_2;
		int32_t L_39 = ___2_size;
		if ((((int32_t)L_38) < ((int32_t)L_39)))
		{
			goto IL_00ac;
		}
	}
	{
		int32_t L_40 = ___2_size;
		V_2 = ((int32_t)il2cpp_codegen_subtract(L_40, 1));
	}

IL_00ac:
	{
		int32_t L_41 = V_3;
		int32_t L_42 = ___2_size;
		if ((((int32_t)L_41) < ((int32_t)L_42)))
		{
			goto IL_00b4;
		}
	}
	{
		int32_t L_43 = ___2_size;
		V_3 = ((int32_t)il2cpp_codegen_subtract(L_43, 1));
	}

IL_00b4:
	{
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_44 = ___4_b;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_45 = ___5_c;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_46 = ___3_a;
		float L_47;
		L_47 = PayloadPortrait_Edge_mE49BB0134C81E064C96AB56D74C59311CF278171(L_44, L_45, L_46, NULL);
		V_4 = L_47;
		float L_48 = V_4;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_49;
		L_49 = fabsf(L_48);
		if ((!(((float)L_49) < ((float)(9.99999975E-05f)))))
		{
			goto IL_00cf;
		}
	}
	{
		return;
	}

IL_00cf:
	{
		int32_t L_50 = V_1;
		V_5 = L_50;
		goto IL_0194;
	}

IL_00d7:
	{
		int32_t L_51 = V_0;
		V_6 = L_51;
		goto IL_0186;
	}

IL_00df:
	{
		int32_t L_52 = V_6;
		int32_t L_53 = V_5;
		Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC((&V_7), ((float)il2cpp_codegen_add(((float)L_52), (0.5f))), ((float)il2cpp_codegen_add(((float)L_53), (0.5f))), (0.0f), NULL);
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_54 = ___4_b;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_55 = ___5_c;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_56 = V_7;
		float L_57;
		L_57 = PayloadPortrait_Edge_mE49BB0134C81E064C96AB56D74C59311CF278171(L_54, L_55, L_56, NULL);
		float L_58 = V_4;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_59 = ___5_c;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_60 = ___3_a;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_61 = V_7;
		float L_62;
		L_62 = PayloadPortrait_Edge_mE49BB0134C81E064C96AB56D74C59311CF278171(L_59, L_60, L_61, NULL);
		float L_63 = V_4;
		V_8 = ((float)(L_62/L_63));
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_64 = ___3_a;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_65 = ___4_b;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_66 = V_7;
		float L_67;
		L_67 = PayloadPortrait_Edge_mE49BB0134C81E064C96AB56D74C59311CF278171(L_64, L_65, L_66, NULL);
		float L_68 = V_4;
		V_9 = ((float)(L_67/L_68));
		if ((((float)((float)(L_57/L_58))) < ((float)(-0.00100000005f))))
		{
			goto IL_0180;
		}
	}
	{
		float L_69 = V_8;
		if ((((float)L_69) < ((float)(-0.00100000005f))))
		{
			goto IL_0180;
		}
	}
	{
		float L_70 = V_9;
		if ((((float)L_70) < ((float)(-0.00100000005f))))
		{
			goto IL_0180;
		}
	}
	{
		int32_t L_71 = V_5;
		int32_t L_72 = ___2_size;
		int32_t L_73 = V_6;
		V_10 = ((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(L_71, L_72)), L_73));
		float L_74 = ___6_z;
		SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* L_75 = ___1_depth;
		int32_t L_76 = V_10;
		NullCheck(L_75);
		int32_t L_77 = L_76;
		float L_78 = (L_75)->GetAt(static_cast<il2cpp_array_size_t>(L_77));
		if ((((float)L_74) <= ((float)L_78)))
		{
			goto IL_0180;
		}
	}
	{
		SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* L_79 = ___1_depth;
		int32_t L_80 = V_10;
		float L_81 = ___6_z;
		NullCheck(L_79);
		(L_79)->SetAt(static_cast<il2cpp_array_size_t>(L_80), (float)L_81);
		int32_t L_82 = V_10;
		V_11 = ((int32_t)il2cpp_codegen_multiply(L_82, 4));
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_83 = ___0_pix;
		int32_t L_84 = V_11;
		uint8_t L_85 = ___7_r;
		NullCheck(L_83);
		(L_83)->SetAt(static_cast<il2cpp_array_size_t>(L_84), (uint8_t)L_85);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_86 = ___0_pix;
		int32_t L_87 = V_11;
		uint8_t L_88 = ___8_g;
		NullCheck(L_86);
		(L_86)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_87, 1))), (uint8_t)L_88);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_89 = ___0_pix;
		int32_t L_90 = V_11;
		uint8_t L_91 = ___9_bl;
		NullCheck(L_89);
		(L_89)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_90, 2))), (uint8_t)L_91);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_92 = ___0_pix;
		int32_t L_93 = V_11;
		NullCheck(L_92);
		(L_92)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_93, 3))), (uint8_t)((int32_t)255));
	}

IL_0180:
	{
		int32_t L_94 = V_6;
		V_6 = ((int32_t)il2cpp_codegen_add(L_94, 1));
	}

IL_0186:
	{
		int32_t L_95 = V_6;
		int32_t L_96 = V_2;
		if ((((int32_t)L_95) <= ((int32_t)L_96)))
		{
			goto IL_00df;
		}
	}
	{
		int32_t L_97 = V_5;
		V_5 = ((int32_t)il2cpp_codegen_add(L_97, 1));
	}

IL_0194:
	{
		int32_t L_98 = V_5;
		int32_t L_99 = V_3;
		if ((((int32_t)L_98) <= ((int32_t)L_99)))
		{
			goto IL_00d7;
		}
	}
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadPortrait_Edge_mE49BB0134C81E064C96AB56D74C59311CF278171 (Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___0_a, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___1_b, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___2_p, const RuntimeMethod* method) 
{
	{
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_0 = ___2_p;
		float L_1 = L_0.___X;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_2 = ___0_a;
		float L_3 = L_2.___X;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_4 = ___1_b;
		float L_5 = L_4.___Y;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_6 = ___0_a;
		float L_7 = L_6.___Y;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_8 = ___2_p;
		float L_9 = L_8.___Y;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_10 = ___0_a;
		float L_11 = L_10.___Y;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_12 = ___1_b;
		float L_13 = L_12.___X;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_14 = ___0_a;
		float L_15 = L_14.___X;
		return ((float)il2cpp_codegen_subtract(((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_subtract(L_1, L_3)), ((float)il2cpp_codegen_subtract(L_5, L_7)))), ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_subtract(L_9, L_11)), ((float)il2cpp_codegen_subtract(L_13, L_15))))));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadPortrait_Ink_m83FD502D90533537A20F3C82A96124A00EBCBF60 (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___0_pix, int32_t ___1_size, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* V_0 = NULL;
	int32_t V_1 = 0;
	int32_t V_2 = 0;
	int32_t V_3 = 0;
	int32_t G_B8_0 = 0;
	{
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_0 = ___0_pix;
		NullCheck((RuntimeArray*)L_0);
		RuntimeObject* L_1;
		L_1 = Array_Clone_m66C9D0727C9BAA0995E4142F29B45BC03582E042((RuntimeArray*)L_0, NULL);
		V_0 = ((ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031*)Castclass((RuntimeObject*)L_1, ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031_il2cpp_TypeInfo_var));
		V_1 = 1;
		goto IL_00ae;
	}

IL_0013:
	{
		V_2 = 1;
		goto IL_00a1;
	}

IL_001a:
	{
		int32_t L_2 = V_1;
		int32_t L_3 = ___1_size;
		int32_t L_4 = V_2;
		V_3 = ((int32_t)il2cpp_codegen_multiply(((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(L_2, L_3)), L_4)), 4));
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_5 = V_0;
		int32_t L_6 = V_3;
		NullCheck(L_5);
		int32_t L_7 = ((int32_t)il2cpp_codegen_add(L_6, 3));
		uint8_t L_8 = (L_5)->GetAt(static_cast<il2cpp_array_size_t>(L_7));
		if (!L_8)
		{
			goto IL_009d;
		}
	}
	{
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_9 = V_0;
		int32_t L_10 = V_1;
		int32_t L_11 = ___1_size;
		int32_t L_12 = V_2;
		NullCheck(L_9);
		int32_t L_13 = ((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(((int32_t)il2cpp_codegen_subtract(L_10, 1)), L_11)), L_12)), 4)), 3));
		uint8_t L_14 = (L_9)->GetAt(static_cast<il2cpp_array_size_t>(L_13));
		if (!L_14)
		{
			goto IL_0068;
		}
	}
	{
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_15 = V_0;
		int32_t L_16 = V_1;
		int32_t L_17 = ___1_size;
		int32_t L_18 = V_2;
		NullCheck(L_15);
		int32_t L_19 = ((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(((int32_t)il2cpp_codegen_add(L_16, 1)), L_17)), L_18)), 4)), 3));
		uint8_t L_20 = (L_15)->GetAt(static_cast<il2cpp_array_size_t>(L_19));
		if (!L_20)
		{
			goto IL_0068;
		}
	}
	{
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_21 = V_0;
		int32_t L_22 = V_1;
		int32_t L_23 = ___1_size;
		int32_t L_24 = V_2;
		NullCheck(L_21);
		int32_t L_25 = ((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(((int32_t)il2cpp_codegen_subtract(((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(L_22, L_23)), L_24)), 1)), 4)), 3));
		uint8_t L_26 = (L_21)->GetAt(static_cast<il2cpp_array_size_t>(L_25));
		if (!L_26)
		{
			goto IL_0068;
		}
	}
	{
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_27 = V_0;
		int32_t L_28 = V_1;
		int32_t L_29 = ___1_size;
		int32_t L_30 = V_2;
		NullCheck(L_27);
		int32_t L_31 = ((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(L_28, L_29)), L_30)), 1)), 4)), 3));
		uint8_t L_32 = (L_27)->GetAt(static_cast<il2cpp_array_size_t>(L_31));
		G_B8_0 = ((((int32_t)L_32) == ((int32_t)0))? 1 : 0);
		goto IL_0069;
	}

IL_0068:
	{
		G_B8_0 = 1;
	}

IL_0069:
	{
		if (!G_B8_0)
		{
			goto IL_009d;
		}
	}
	{
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_33 = ___0_pix;
		int32_t L_34 = V_3;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_35 = ___0_pix;
		int32_t L_36 = V_3;
		NullCheck(L_35);
		int32_t L_37 = L_36;
		uint8_t L_38 = (L_35)->GetAt(static_cast<il2cpp_array_size_t>(L_37));
		NullCheck(L_33);
		(L_33)->SetAt(static_cast<il2cpp_array_size_t>(L_34), (uint8_t)il2cpp_codegen_cast_floating_point<uint8_t, int32_t, float>(((float)il2cpp_codegen_multiply(((float)L_38), (0.349999994f)))));
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_39 = ___0_pix;
		int32_t L_40 = V_3;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_41 = ___0_pix;
		int32_t L_42 = V_3;
		NullCheck(L_41);
		int32_t L_43 = ((int32_t)il2cpp_codegen_add(L_42, 1));
		uint8_t L_44 = (L_41)->GetAt(static_cast<il2cpp_array_size_t>(L_43));
		NullCheck(L_39);
		(L_39)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_40, 1))), (uint8_t)il2cpp_codegen_cast_floating_point<uint8_t, int32_t, float>(((float)il2cpp_codegen_multiply(((float)L_44), (0.349999994f)))));
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_45 = ___0_pix;
		int32_t L_46 = V_3;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_47 = ___0_pix;
		int32_t L_48 = V_3;
		NullCheck(L_47);
		int32_t L_49 = ((int32_t)il2cpp_codegen_add(L_48, 2));
		uint8_t L_50 = (L_47)->GetAt(static_cast<il2cpp_array_size_t>(L_49));
		NullCheck(L_45);
		(L_45)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_46, 2))), (uint8_t)il2cpp_codegen_cast_floating_point<uint8_t, int32_t, float>(((float)il2cpp_codegen_multiply(((float)L_50), (0.349999994f)))));
	}

IL_009d:
	{
		int32_t L_51 = V_2;
		V_2 = ((int32_t)il2cpp_codegen_add(L_51, 1));
	}

IL_00a1:
	{
		int32_t L_52 = V_2;
		int32_t L_53 = ___1_size;
		if ((((int32_t)L_52) < ((int32_t)((int32_t)il2cpp_codegen_subtract(L_53, 1)))))
		{
			goto IL_001a;
		}
	}
	{
		int32_t L_54 = V_1;
		V_1 = ((int32_t)il2cpp_codegen_add(L_54, 1));
	}

IL_00ae:
	{
		int32_t L_55 = V_1;
		int32_t L_56 = ___1_size;
		if ((((int32_t)L_55) < ((int32_t)((int32_t)il2cpp_codegen_subtract(L_56, 1)))))
		{
			goto IL_0013;
		}
	}
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadPortrait_Dot_m3B2C1EFC28B630AD386621635E287771DB77CA7E (Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___0_a, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___1_b, const RuntimeMethod* method) 
{
	{
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_0 = ___0_a;
		float L_1 = L_0.___X;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_2 = ___1_b;
		float L_3 = L_2.___X;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_4 = ___0_a;
		float L_5 = L_4.___Y;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_6 = ___1_b;
		float L_7 = L_6.___Y;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_8 = ___0_a;
		float L_9 = L_8.___Z;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_10 = ___1_b;
		float L_11 = L_10.___Z;
		return ((float)il2cpp_codegen_add(((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(L_1, L_3)), ((float)il2cpp_codegen_multiply(L_5, L_7)))), ((float)il2cpp_codegen_multiply(L_9, L_11))));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 PayloadPortrait_Cross_mE37122B76B6C8A2998DB6F4AADC3B52E868C18FA (Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___0_a, Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___1_b, const RuntimeMethod* method) 
{
	{
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_0 = ___0_a;
		float L_1 = L_0.___Y;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_2 = ___1_b;
		float L_3 = L_2.___Z;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_4 = ___0_a;
		float L_5 = L_4.___Z;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_6 = ___1_b;
		float L_7 = L_6.___Y;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_8 = ___0_a;
		float L_9 = L_8.___Z;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_10 = ___1_b;
		float L_11 = L_10.___X;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_12 = ___0_a;
		float L_13 = L_12.___X;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_14 = ___1_b;
		float L_15 = L_14.___Z;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_16 = ___0_a;
		float L_17 = L_16.___X;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_18 = ___1_b;
		float L_19 = L_18.___Y;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_20 = ___0_a;
		float L_21 = L_20.___Y;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_22 = ___1_b;
		float L_23 = L_22.___X;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_24;
		memset((&L_24), 0, sizeof(L_24));
		Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC((&L_24), ((float)il2cpp_codegen_subtract(((float)il2cpp_codegen_multiply(L_1, L_3)), ((float)il2cpp_codegen_multiply(L_5, L_7)))), ((float)il2cpp_codegen_subtract(((float)il2cpp_codegen_multiply(L_9, L_11)), ((float)il2cpp_codegen_multiply(L_13, L_15)))), ((float)il2cpp_codegen_subtract(((float)il2cpp_codegen_multiply(L_17, L_19)), ((float)il2cpp_codegen_multiply(L_21, L_23)))), NULL);
		return L_24;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 PayloadPortrait_Norm_m29C7521F65F744765AAD755AB48176D383F5C4DE (Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 ___0_v, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	{
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_0 = ___0_v;
		float L_1 = L_0.___X;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_2 = ___0_v;
		float L_3 = L_2.___X;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_4 = ___0_v;
		float L_5 = L_4.___Y;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_6 = ___0_v;
		float L_7 = L_6.___Y;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_8 = ___0_v;
		float L_9 = L_8.___Z;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_10 = ___0_v;
		float L_11 = L_10.___Z;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_12;
		L_12 = sqrt(((double)((float)il2cpp_codegen_add(((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(L_1, L_3)), ((float)il2cpp_codegen_multiply(L_5, L_7)))), ((float)il2cpp_codegen_multiply(L_9, L_11))))));
		V_0 = ((float)L_12);
		float L_13 = V_0;
		if ((!(((float)L_13) < ((float)(9.99999997E-07f)))))
		{
			goto IL_004e;
		}
	}
	{
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_14;
		memset((&L_14), 0, sizeof(L_14));
		Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC((&L_14), (0.0f), (0.0f), (1.0f), NULL);
		return L_14;
	}

IL_004e:
	{
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_15 = ___0_v;
		float L_16 = L_15.___X;
		float L_17 = V_0;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_18 = ___0_v;
		float L_19 = L_18.___Y;
		float L_20 = V_0;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_21 = ___0_v;
		float L_22 = L_21.___Z;
		float L_23 = V_0;
		Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109 L_24;
		memset((&L_24), 0, sizeof(L_24));
		Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC((&L_24), ((float)(L_16/L_17)), ((float)(L_19/L_20)), ((float)(L_22/L_23)), NULL);
		return L_24;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* PayloadPortrait_Ihdr_mA24744E56AEA81D70DBD8289B77A595EB1A07C16 (int32_t ___0_w, int32_t ___1_h, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_0 = (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031*)(ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031*)SZArrayNew(ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031_il2cpp_TypeInfo_var, (uint32_t)((int32_t)13));
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_1 = L_0;
		int32_t L_2 = ___0_w;
		PayloadPortrait_Be_m64E1989BA42015D7FC4F7A554D8466B6137111E4(L_1, 0, L_2, NULL);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_3 = L_1;
		int32_t L_4 = ___1_h;
		PayloadPortrait_Be_m64E1989BA42015D7FC4F7A554D8466B6137111E4(L_3, 4, L_4, NULL);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_5 = L_3;
		NullCheck(L_5);
		(L_5)->SetAt(static_cast<il2cpp_array_size_t>(8), (uint8_t)8);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_6 = L_5;
		NullCheck(L_6);
		(L_6)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)9)), (uint8_t)6);
		return L_6;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t PayloadPortrait_Chunk_m41C41C7B18F101C865A33CEA318FB20276027F44 (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___0_png, int32_t ___1_o, String_t* ___2_type, ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___3_data, const RuntimeMethod* method) 
{
	uint32_t V_0 = 0;
	{
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_0 = ___0_png;
		int32_t L_1 = ___1_o;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_2 = ___3_data;
		NullCheck(L_2);
		PayloadPortrait_Be_m64E1989BA42015D7FC4F7A554D8466B6137111E4(L_0, L_1, ((int32_t)(((RuntimeArray*)L_2)->max_length)), NULL);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_3 = ___0_png;
		int32_t L_4 = ___1_o;
		String_t* L_5 = ___2_type;
		NullCheck(L_5);
		Il2CppChar L_6;
		L_6 = String_get_Chars_mC49DF0CD2D3BE7BE97B3AD9C995BE3094F8E36D3(L_5, 0, NULL);
		NullCheck(L_3);
		(L_3)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_4, 4))), (uint8_t)((int32_t)(uint8_t)L_6));
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_7 = ___0_png;
		int32_t L_8 = ___1_o;
		String_t* L_9 = ___2_type;
		NullCheck(L_9);
		Il2CppChar L_10;
		L_10 = String_get_Chars_mC49DF0CD2D3BE7BE97B3AD9C995BE3094F8E36D3(L_9, 1, NULL);
		NullCheck(L_7);
		(L_7)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_8, 5))), (uint8_t)((int32_t)(uint8_t)L_10));
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_11 = ___0_png;
		int32_t L_12 = ___1_o;
		String_t* L_13 = ___2_type;
		NullCheck(L_13);
		Il2CppChar L_14;
		L_14 = String_get_Chars_mC49DF0CD2D3BE7BE97B3AD9C995BE3094F8E36D3(L_13, 2, NULL);
		NullCheck(L_11);
		(L_11)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_12, 6))), (uint8_t)((int32_t)(uint8_t)L_14));
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_15 = ___0_png;
		int32_t L_16 = ___1_o;
		String_t* L_17 = ___2_type;
		NullCheck(L_17);
		Il2CppChar L_18;
		L_18 = String_get_Chars_mC49DF0CD2D3BE7BE97B3AD9C995BE3094F8E36D3(L_17, 3, NULL);
		NullCheck(L_15);
		(L_15)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_16, 7))), (uint8_t)((int32_t)(uint8_t)L_18));
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_19 = ___3_data;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_20 = ___0_png;
		int32_t L_21 = ___1_o;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_22 = ___3_data;
		NullCheck(L_22);
		Buffer_BlockCopy_m2F7BC0C5BA97C500E3F87D5008718F797E02B358((RuntimeArray*)L_19, 0, (RuntimeArray*)L_20, ((int32_t)il2cpp_codegen_add(L_21, 8)), ((int32_t)(((RuntimeArray*)L_22)->max_length)), NULL);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_23 = ___0_png;
		int32_t L_24 = ___1_o;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_25 = ___3_data;
		NullCheck(L_25);
		uint32_t L_26;
		L_26 = PayloadPortrait_Crc_m6805124043E0DD4FE8BF6B74295A01D263347692(L_23, ((int32_t)il2cpp_codegen_add(L_24, 4)), ((int32_t)il2cpp_codegen_add(4, ((int32_t)(((RuntimeArray*)L_25)->max_length)))), NULL);
		V_0 = L_26;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_27 = ___0_png;
		int32_t L_28 = ___1_o;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_29 = ___3_data;
		NullCheck(L_29);
		uint32_t L_30 = V_0;
		PayloadPortrait_Be_m64E1989BA42015D7FC4F7A554D8466B6137111E4(L_27, ((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_add(L_28, 8)), ((int32_t)(((RuntimeArray*)L_29)->max_length)))), L_30, NULL);
		int32_t L_31 = ___1_o;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_32 = ___3_data;
		NullCheck(L_32);
		return ((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_add(L_31, ((int32_t)12))), ((int32_t)(((RuntimeArray*)L_32)->max_length))));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadPortrait_Be_m64E1989BA42015D7FC4F7A554D8466B6137111E4 (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___0_b, int32_t ___1_o, int32_t ___2_v, const RuntimeMethod* method) 
{
	{
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_0 = ___0_b;
		int32_t L_1 = ___1_o;
		int32_t L_2 = ___2_v;
		NullCheck(L_0);
		(L_0)->SetAt(static_cast<il2cpp_array_size_t>(L_1), (uint8_t)((int32_t)(uint8_t)((int32_t)(L_2>>((int32_t)24)))));
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_3 = ___0_b;
		int32_t L_4 = ___1_o;
		int32_t L_5 = ___2_v;
		NullCheck(L_3);
		(L_3)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_4, 1))), (uint8_t)((int32_t)(uint8_t)((int32_t)(L_5>>((int32_t)16)))));
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_6 = ___0_b;
		int32_t L_7 = ___1_o;
		int32_t L_8 = ___2_v;
		NullCheck(L_6);
		(L_6)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_7, 2))), (uint8_t)((int32_t)(uint8_t)((int32_t)(L_8>>8))));
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_9 = ___0_b;
		int32_t L_10 = ___1_o;
		int32_t L_11 = ___2_v;
		NullCheck(L_9);
		(L_9)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_10, 3))), (uint8_t)((int32_t)(uint8_t)L_11));
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* PayloadPortrait_Zlib_mBDCD2A945DE849A569570D76237699D522FB2A28 (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___0_raw, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&DeflateStream_tF1758952E9DBAB2F9A15D42971F33A78AB4FC104_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&MemoryStream_tAAED1B42172E3390584E4194308AB878E786AAC2_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	MemoryStream_tAAED1B42172E3390584E4194308AB878E786AAC2* V_0 = NULL;
	uint32_t V_1 = 0;
	DeflateStream_tF1758952E9DBAB2F9A15D42971F33A78AB4FC104* V_2 = NULL;
	ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* V_3 = NULL;
	{
		MemoryStream_tAAED1B42172E3390584E4194308AB878E786AAC2* L_0 = (MemoryStream_tAAED1B42172E3390584E4194308AB878E786AAC2*)il2cpp_codegen_object_new(MemoryStream_tAAED1B42172E3390584E4194308AB878E786AAC2_il2cpp_TypeInfo_var);
		MemoryStream__ctor_m8F3BAE0B48E65BAA13C52FB020E502B3EA22CA6B(L_0, NULL);
		V_0 = L_0;
	}
	{
		auto __finallyBlock = il2cpp::utils::Finally([&]
		{

FINALLY_0071:
			{
				{
					MemoryStream_tAAED1B42172E3390584E4194308AB878E786AAC2* L_1 = V_0;
					if (!L_1)
					{
						goto IL_007a;
					}
				}
				{
					MemoryStream_tAAED1B42172E3390584E4194308AB878E786AAC2* L_2 = V_0;
					NullCheck(L_2);
					InterfaceActionInvoker0::Invoke(0, IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var, L_2);
				}

IL_007a:
				{
					return;
				}
			}
		});
		try
		{
			{
				MemoryStream_tAAED1B42172E3390584E4194308AB878E786AAC2* L_3 = V_0;
				NullCheck(L_3);
				VirtualActionInvoker1< uint8_t >::Invoke(27, L_3, (uint8_t)((int32_t)120));
				MemoryStream_tAAED1B42172E3390584E4194308AB878E786AAC2* L_4 = V_0;
				NullCheck(L_4);
				VirtualActionInvoker1< uint8_t >::Invoke(27, L_4, (uint8_t)((int32_t)156));
				MemoryStream_tAAED1B42172E3390584E4194308AB878E786AAC2* L_5 = V_0;
				DeflateStream_tF1758952E9DBAB2F9A15D42971F33A78AB4FC104* L_6 = (DeflateStream_tF1758952E9DBAB2F9A15D42971F33A78AB4FC104*)il2cpp_codegen_object_new(DeflateStream_tF1758952E9DBAB2F9A15D42971F33A78AB4FC104_il2cpp_TypeInfo_var);
				DeflateStream__ctor_m55E861339218B06FD5B41D4DCAF09E598AF0C074(L_6, L_5, 1, (bool)1, NULL);
				V_2 = L_6;
			}
			{
				auto __finallyBlock = il2cpp::utils::Finally([&]
				{

FINALLY_002f_1:
					{
						{
							DeflateStream_tF1758952E9DBAB2F9A15D42971F33A78AB4FC104* L_7 = V_2;
							if (!L_7)
							{
								goto IL_0038_1;
							}
						}
						{
							DeflateStream_tF1758952E9DBAB2F9A15D42971F33A78AB4FC104* L_8 = V_2;
							NullCheck(L_8);
							InterfaceActionInvoker0::Invoke(0, IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var, L_8);
						}

IL_0038_1:
						{
							return;
						}
					}
				});
				try
				{
					DeflateStream_tF1758952E9DBAB2F9A15D42971F33A78AB4FC104* L_9 = V_2;
					ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_10 = ___0_raw;
					ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_11 = ___0_raw;
					NullCheck(L_11);
					NullCheck(L_9);
					VirtualActionInvoker3< ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031*, int32_t, int32_t >::Invoke(25, L_9, L_10, 0, ((int32_t)(((RuntimeArray*)L_11)->max_length)));
					goto IL_0039_1;
				}
				catch(Il2CppExceptionWrapper& e)
				{
					__finallyBlock.StoreException(e.ex);
				}
			}

IL_0039_1:
			{
				ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_12 = ___0_raw;
				uint32_t L_13;
				L_13 = PayloadPortrait_Adler_m6D2D1A1F00857BA4BB3740C4C63916E5CB36A740(L_12, NULL);
				V_1 = L_13;
				MemoryStream_tAAED1B42172E3390584E4194308AB878E786AAC2* L_14 = V_0;
				uint32_t L_15 = V_1;
				NullCheck(L_14);
				VirtualActionInvoker1< uint8_t >::Invoke(27, L_14, (uint8_t)((int32_t)(uint8_t)((int32_t)((uint32_t)L_15>>((int32_t)24)))));
				MemoryStream_tAAED1B42172E3390584E4194308AB878E786AAC2* L_16 = V_0;
				uint32_t L_17 = V_1;
				NullCheck(L_16);
				VirtualActionInvoker1< uint8_t >::Invoke(27, L_16, (uint8_t)((int32_t)(uint8_t)((int32_t)((uint32_t)L_17>>((int32_t)16)))));
				MemoryStream_tAAED1B42172E3390584E4194308AB878E786AAC2* L_18 = V_0;
				uint32_t L_19 = V_1;
				NullCheck(L_18);
				VirtualActionInvoker1< uint8_t >::Invoke(27, L_18, (uint8_t)((int32_t)(uint8_t)((int32_t)((uint32_t)L_19>>8))));
				MemoryStream_tAAED1B42172E3390584E4194308AB878E786AAC2* L_20 = V_0;
				uint32_t L_21 = V_1;
				NullCheck(L_20);
				VirtualActionInvoker1< uint8_t >::Invoke(27, L_20, (uint8_t)((int32_t)(uint8_t)L_21));
				MemoryStream_tAAED1B42172E3390584E4194308AB878E786AAC2* L_22 = V_0;
				NullCheck(L_22);
				ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_23;
				L_23 = VirtualFuncInvoker0< ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* >::Invoke(31, L_22);
				V_3 = L_23;
				goto IL_007b;
			}
		}
		catch(Il2CppExceptionWrapper& e)
		{
			__finallyBlock.StoreException(e.ex);
		}
	}

IL_007b:
	{
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_24 = V_3;
		return L_24;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t PayloadPortrait_Adler_m6D2D1A1F00857BA4BB3740C4C63916E5CB36A740 (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___0_data, const RuntimeMethod* method) 
{
	uint32_t V_0 = 0;
	uint32_t V_1 = 0;
	int32_t V_2 = 0;
	{
		V_0 = 1;
		V_1 = 0;
		V_2 = 0;
		goto IL_0022;
	}

IL_0008:
	{
		uint32_t L_0 = V_0;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_1 = ___0_data;
		int32_t L_2 = V_2;
		NullCheck(L_1);
		int32_t L_3 = L_2;
		uint8_t L_4 = (L_1)->GetAt(static_cast<il2cpp_array_size_t>(L_3));
		V_0 = ((int32_t)((uint32_t)(int32_t)((int32_t)il2cpp_codegen_add((int32_t)L_0, (int32_t)L_4))%(uint32_t)(int32_t)((int32_t)65521)));
		uint32_t L_5 = V_1;
		uint32_t L_6 = V_0;
		V_1 = ((int32_t)((uint32_t)(int32_t)((int32_t)il2cpp_codegen_add((int32_t)L_5, (int32_t)L_6))%(uint32_t)(int32_t)((int32_t)65521)));
		int32_t L_7 = V_2;
		V_2 = ((int32_t)il2cpp_codegen_add(L_7, 1));
	}

IL_0022:
	{
		int32_t L_8 = V_2;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_9 = ___0_data;
		NullCheck(L_9);
		if ((((int32_t)L_8) < ((int32_t)((int32_t)(((RuntimeArray*)L_9)->max_length)))))
		{
			goto IL_0008;
		}
	}
	{
		uint32_t L_10 = V_1;
		uint32_t L_11 = V_0;
		return ((int32_t)(((int32_t)((int32_t)L_10<<((int32_t)16)))|(int32_t)L_11));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t PayloadPortrait_Crc_m6805124043E0DD4FE8BF6B74295A01D263347692 (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___0_data, int32_t ___1_start, int32_t ___2_len, const RuntimeMethod* method) 
{
	uint32_t V_0 = 0;
	int32_t V_1 = 0;
	int32_t V_2 = 0;
	uint32_t G_B5_0 = 0;
	{
		V_0 = (-1);
		V_1 = 0;
		goto IL_0032;
	}

IL_0006:
	{
		uint32_t L_0 = V_0;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_1 = ___0_data;
		int32_t L_2 = ___1_start;
		int32_t L_3 = V_1;
		NullCheck(L_1);
		int32_t L_4 = ((int32_t)il2cpp_codegen_add(L_2, L_3));
		uint8_t L_5 = (L_1)->GetAt(static_cast<il2cpp_array_size_t>(L_4));
		V_0 = ((int32_t)((int32_t)L_0^(int32_t)L_5));
		V_2 = 0;
		goto IL_002a;
	}

IL_0012:
	{
		uint32_t L_6 = V_0;
		if (((int32_t)((int32_t)L_6&1)))
		{
			goto IL_001c;
		}
	}
	{
		uint32_t L_7 = V_0;
		G_B5_0 = ((uint32_t)(((int32_t)((uint32_t)L_7>>1))));
		goto IL_0025;
	}

IL_001c:
	{
		uint32_t L_8 = V_0;
		G_B5_0 = ((uint32_t)(((int32_t)(((int32_t)-306674912)^((int32_t)((uint32_t)L_8>>1))))));
	}

IL_0025:
	{
		V_0 = G_B5_0;
		int32_t L_9 = V_2;
		V_2 = ((int32_t)il2cpp_codegen_add(L_9, 1));
	}

IL_002a:
	{
		int32_t L_10 = V_2;
		if ((((int32_t)L_10) < ((int32_t)8)))
		{
			goto IL_0012;
		}
	}
	{
		int32_t L_11 = V_1;
		V_1 = ((int32_t)il2cpp_codegen_add(L_11, 1));
	}

IL_0032:
	{
		int32_t L_12 = V_1;
		int32_t L_13 = ___2_len;
		if ((((int32_t)L_12) < ((int32_t)L_13)))
		{
			goto IL_0006;
		}
	}
	{
		uint32_t L_14 = V_0;
		return ((int32_t)((int32_t)L_14^(-1)));
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
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t FaceOrder_Compare_m3C9E5D34043A8941A6167914C7CA673AABEF8796 (FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752* __this, Face_tDD2362557504D5249B24936FC555B3D54E56E6BE ___0_x, Face_tDD2362557504D5249B24936FC555B3D54E56E6BE ___1_y, const RuntimeMethod* method) 
{
	{
		float* L_0 = (float*)(&(&___0_x)->___Z);
		Face_tDD2362557504D5249B24936FC555B3D54E56E6BE L_1 = ___1_y;
		float L_2 = L_1.___Z;
		int32_t L_3;
		L_3 = Single_CompareTo_m06F7868162EB392D3E99103D1A0BD27463C9E66F(L_0, L_2, NULL);
		return L_3;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void FaceOrder__ctor_mF221956DD9BEBFB416D9DB8BF0E77B2CF1F6B04E (FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752* __this, const RuntimeMethod* method) 
{
	{
		Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2(__this, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void FaceOrder__cctor_m7A2015C79D336FE4EE100645513ECF8F6EC89089 (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752* L_0 = (FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752*)il2cpp_codegen_object_new(FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752_il2cpp_TypeInfo_var);
		FaceOrder__ctor_mF221956DD9BEBFB416D9DB8BF0E77B2CF1F6B04E(L_0, NULL);
		((FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752_StaticFields*)il2cpp_codegen_static_fields_for(FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752_il2cpp_TypeInfo_var))->___FarFirst = L_0;
		Il2CppCodeGenWriteBarrier((void**)(&((FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752_StaticFields*)il2cpp_codegen_static_fields_for(FaceOrder_t3C96C78C10757B5235BC173C6BCFB85ED1587752_il2cpp_TypeInfo_var))->___FarFirst), (void*)L_0);
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
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC (Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109* __this, float ___0_x, float ___1_y, float ___2_z, const RuntimeMethod* method) 
{
	{
		float L_0 = ___0_x;
		__this->___X = L_0;
		float L_1 = ___1_y;
		__this->___Y = L_1;
		float L_2 = ___2_z;
		__this->___Z = L_2;
		return;
	}
}
IL2CPP_EXTERN_C  void Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC_AdjustorThunk (RuntimeObject* __this, float ___0_x, float ___1_y, float ___2_z, const RuntimeMethod* method)
{
	Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<Vec_t6FF44BE2ECBBB31B911615F7615D48064399B109*>(__this + _offset);
	Vec__ctor_m64925B43A5F62280A4FEF129D28CAEE68D5247DC(_thisAdjusted, ___0_x, ___1_y, ___2_z, method);
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
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Job_t0B1165E35BF83E24991BB9F20783AADD55C95267* PayloadRender_Begin_m53C02A7347E53AA9D70862D97553B84CEA532255 (String_t* ___0_form, int32_t ___1_px, int32_t ___2_backdrop, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Job_t0B1165E35BF83E24991BB9F20783AADD55C95267_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		String_t* L_0 = ___0_form;
		int32_t L_1 = ___1_px;
		int32_t L_2 = ___2_backdrop;
		Job_t0B1165E35BF83E24991BB9F20783AADD55C95267* L_3 = (Job_t0B1165E35BF83E24991BB9F20783AADD55C95267*)il2cpp_codegen_object_new(Job_t0B1165E35BF83E24991BB9F20783AADD55C95267_il2cpp_TypeInfo_var);
		Job__ctor_m89419F39EB941FDAA2D4CA1E62E8EE9C3A0FE7EE(L_3, L_0, L_1, L_2, NULL);
		return L_3;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* PayloadRender_get_Types_m7C7FF970100DDE704D3F021C36D2AA59FD1800B0 (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		RuntimeObject* L_0;
		L_0 = PayloadForms_get_Ids_mC28791119EB2FF88D4CDBDB1C881C351BDC5E17E_inline(NULL);
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* PayloadRender_Rgba_mBF3297E7D7F7F9B9A105BFA1EFC58C29381CBED2 (String_t* ___0_form, int32_t ___1_px, int32_t ___2_backdrop, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Job_t0B1165E35BF83E24991BB9F20783AADD55C95267_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	Job_t0B1165E35BF83E24991BB9F20783AADD55C95267* V_0 = NULL;
	{
		String_t* L_0 = ___0_form;
		int32_t L_1 = ___1_px;
		int32_t L_2 = ___2_backdrop;
		Job_t0B1165E35BF83E24991BB9F20783AADD55C95267* L_3 = (Job_t0B1165E35BF83E24991BB9F20783AADD55C95267*)il2cpp_codegen_object_new(Job_t0B1165E35BF83E24991BB9F20783AADD55C95267_il2cpp_TypeInfo_var);
		Job__ctor_m89419F39EB941FDAA2D4CA1E62E8EE9C3A0FE7EE(L_3, L_0, L_1, L_2, NULL);
		V_0 = L_3;
	}

IL_0009:
	{
		Job_t0B1165E35BF83E24991BB9F20783AADD55C95267* L_4 = V_0;
		NullCheck(L_4);
		bool L_5;
		L_5 = Job_Step_mA354313D818FA7693E6076FBB886FB20772E675A(L_4, ((int32_t)2147483647LL), NULL);
		if (!L_5)
		{
			goto IL_0009;
		}
	}
	{
		Job_t0B1165E35BF83E24991BB9F20783AADD55C95267* L_6 = V_0;
		NullCheck(L_6);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_7 = L_6->___Pixels;
		return L_7;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* PayloadRender_Png_mA1EC3FF87A48EF518128979DDF812EC32F0E14AD (String_t* ___0_form, int32_t ___1_px, int32_t ___2_backdrop, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		int32_t L_0 = ___1_px;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		int32_t L_1;
		L_1 = PayloadRender_Clamp_m031B01B75934395207D92A93B1208E9EA1905F81(L_0, ((int32_t)16), ((int32_t)1024), NULL);
		int32_t L_2 = ___1_px;
		int32_t L_3;
		L_3 = PayloadRender_Clamp_m031B01B75934395207D92A93B1208E9EA1905F81(L_2, ((int32_t)16), ((int32_t)1024), NULL);
		String_t* L_4 = ___0_form;
		int32_t L_5 = ___1_px;
		int32_t L_6 = ___2_backdrop;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_7;
		L_7 = PayloadRender_Rgba_mBF3297E7D7F7F9B9A105BFA1EFC58C29381CBED2(L_4, L_5, L_6, NULL);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_8;
		L_8 = PayloadPortrait_EncodePng_m70BAD5C2F9C8A28CF61592C8CC058E3EFEF2DC08(L_1, L_3, L_7, NULL);
		return L_8;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double PayloadRender_LumaVariance_m36AA7E371CC62A3F3A8BEC5F939DF4D2F50B9D52 (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* ___0_rgba, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	double V_0 = 0.0;
	double V_1 = 0.0;
	int64_t V_2 = 0;
	double V_3 = 0.0;
	int32_t V_4 = 0;
	double V_5 = 0.0;
	{
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_0 = ___0_rgba;
		if (!L_0)
		{
			goto IL_0009;
		}
	}
	{
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_1 = ___0_rgba;
		NullCheck(L_1);
		if ((((int32_t)((int32_t)(((RuntimeArray*)L_1)->max_length))) >= ((int32_t)4)))
		{
			goto IL_0013;
		}
	}

IL_0009:
	{
		return (0.0);
	}

IL_0013:
	{
		V_0 = (0.0);
		V_1 = (0.0);
		V_2 = ((int64_t)0);
		V_4 = 0;
		goto IL_008f;
	}

IL_002f:
	{
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_2 = ___0_rgba;
		int32_t L_3 = V_4;
		NullCheck(L_2);
		int32_t L_4 = ((int32_t)il2cpp_codegen_add(L_3, 3));
		uint8_t L_5 = (L_2)->GetAt(static_cast<il2cpp_array_size_t>(L_4));
		if ((((int32_t)L_5) < ((int32_t)8)))
		{
			goto IL_0089;
		}
	}
	{
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_6 = ___0_rgba;
		int32_t L_7 = V_4;
		NullCheck(L_6);
		int32_t L_8 = L_7;
		uint8_t L_9 = (L_6)->GetAt(static_cast<il2cpp_array_size_t>(L_8));
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_10 = ___0_rgba;
		int32_t L_11 = V_4;
		NullCheck(L_10);
		int32_t L_12 = ((int32_t)il2cpp_codegen_add(L_11, 1));
		uint8_t L_13 = (L_10)->GetAt(static_cast<il2cpp_array_size_t>(L_12));
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_14 = ___0_rgba;
		int32_t L_15 = V_4;
		NullCheck(L_14);
		int32_t L_16 = ((int32_t)il2cpp_codegen_add(L_15, 2));
		uint8_t L_17 = (L_14)->GetAt(static_cast<il2cpp_array_size_t>(L_16));
		V_5 = ((double)(((double)il2cpp_codegen_add(((double)il2cpp_codegen_add(((double)il2cpp_codegen_multiply((0.21260000000000001), ((double)L_9))), ((double)il2cpp_codegen_multiply((0.71519999999999995), ((double)L_13))))), ((double)il2cpp_codegen_multiply((0.0722), ((double)L_17)))))/(255.0)));
		double L_18 = V_0;
		double L_19 = V_5;
		V_0 = ((double)il2cpp_codegen_add(L_18, L_19));
		double L_20 = V_1;
		double L_21 = V_5;
		double L_22 = V_5;
		V_1 = ((double)il2cpp_codegen_add(L_20, ((double)il2cpp_codegen_multiply(L_21, L_22))));
		int64_t L_23 = V_2;
		V_2 = ((int64_t)il2cpp_codegen_add(L_23, ((int64_t)1)));
	}

IL_0089:
	{
		int32_t L_24 = V_4;
		V_4 = ((int32_t)il2cpp_codegen_add(L_24, 4));
	}

IL_008f:
	{
		int32_t L_25 = V_4;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_26 = ___0_rgba;
		NullCheck(L_26);
		if ((((int32_t)((int32_t)il2cpp_codegen_add(L_25, 3))) < ((int32_t)((int32_t)(((RuntimeArray*)L_26)->max_length)))))
		{
			goto IL_002f;
		}
	}
	{
		int64_t L_27 = V_2;
		if ((((int64_t)L_27) >= ((int64_t)((int64_t)2))))
		{
			goto IL_00a7;
		}
	}
	{
		return (0.0);
	}

IL_00a7:
	{
		double L_28 = V_0;
		int64_t L_29 = V_2;
		V_3 = ((double)(L_28/((double)L_29)));
		double L_30 = V_1;
		int64_t L_31 = V_2;
		double L_32 = V_3;
		double L_33 = V_3;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_34;
		L_34 = Math_Max_m7BAC743E1752A51F258BB82DEBDD13E7C6D3ED26((0.0), ((double)il2cpp_codegen_subtract(((double)(L_30/((double)L_31))), ((double)il2cpp_codegen_multiply(L_32, L_33)))), NULL);
		return L_34;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadRender_Coverage_m3536A32FB528FFC6AC348FBEE28906AEAD668122 (String_t* ___0_form, int32_t ___1_px, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* V_0 = NULL;
	Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 V_1;
	memset((&V_1), 0, sizeof(V_1));
	int32_t V_2 = 0;
	int32_t V_3 = 0;
	int32_t V_4 = 0;
	int32_t V_5 = 0;
	{
		String_t* L_0 = ___0_form;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_1;
		L_1 = PayloadRender_Scene_mF202D14509424834B06B24444ADF5AC9BCD5B49A(L_0, NULL);
		V_0 = L_1;
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_2 = V_0;
		Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_3;
		L_3 = PayloadRender_Frame_mACB603D0B07928A49BEDD4335DC3A98C01CB8196(L_2, NULL);
		V_1 = L_3;
		V_2 = 0;
		V_3 = 0;
		goto IL_0053;
	}

IL_0014:
	{
		V_4 = 0;
		goto IL_004a;
	}

IL_0019:
	{
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_4 = V_0;
		Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_5 = V_1;
		int32_t L_6 = V_4;
		int32_t L_7 = ___1_px;
		int32_t L_8 = V_3;
		int32_t L_9 = ___1_px;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F L_10;
		L_10 = PayloadRender_Sample_mE7A4F8364A54709D1A58A600D4D029ED4B123C4A(L_4, L_5, ((float)(((float)il2cpp_codegen_add(((float)L_6), (0.5f)))/((float)L_7))), ((float)(((float)il2cpp_codegen_add(((float)L_8), (0.5f)))/((float)L_9))), 1, (&V_5), NULL);
		int32_t L_11 = V_5;
		if ((((int32_t)L_11) < ((int32_t)0)))
		{
			goto IL_0044;
		}
	}
	{
		int32_t L_12 = V_2;
		V_2 = ((int32_t)il2cpp_codegen_add(L_12, 1));
	}

IL_0044:
	{
		int32_t L_13 = V_4;
		V_4 = ((int32_t)il2cpp_codegen_add(L_13, 1));
	}

IL_004a:
	{
		int32_t L_14 = V_4;
		int32_t L_15 = ___1_px;
		if ((((int32_t)L_14) < ((int32_t)L_15)))
		{
			goto IL_0019;
		}
	}
	{
		int32_t L_16 = V_3;
		V_3 = ((int32_t)il2cpp_codegen_add(L_16, 1));
	}

IL_0053:
	{
		int32_t L_17 = V_3;
		int32_t L_18 = ___1_px;
		if ((((int32_t)L_17) < ((int32_t)L_18)))
		{
			goto IL_0014;
		}
	}
	{
		int32_t L_19 = V_2;
		int32_t L_20 = ___1_px;
		int32_t L_21 = ___1_px;
		return ((float)(((float)L_19)/((float)((int32_t)il2cpp_codegen_multiply(L_20, L_21)))));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 PayloadRender_Frame_mACB603D0B07928A49BEDD4335DC3A98C01CB8196 (List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* ___0_scene, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Enumerator_Dispose_mF1558E7B7B84D786841C7B736ECC77BEED4B1367_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Enumerator_MoveNext_mD22EC89FDB16CDABC6AB475A8E71C56858023AC5_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Enumerator_get_Current_m7673828665C413CFC1943B4A6DB0F0148D99596B_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerable_1_t0AA6705B9378A4033A24C2FB0A50C35A32204416_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerator_1_tD99202564DB6FD9B170E7069409E5370D6604F6B_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_GetEnumerator_mCD2F70F8886ECEF58C2B0ACD4CC85F99A80733B7_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_0;
	memset((&V_0), 0, sizeof(V_0));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_1;
	memset((&V_1), 0, sizeof(V_1));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_2;
	memset((&V_2), 0, sizeof(V_2));
	float V_3 = 0.0f;
	Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 V_4;
	memset((&V_4), 0, sizeof(V_4));
	float V_5 = 0.0f;
	float V_6 = 0.0f;
	float V_7 = 0.0f;
	float V_8 = 0.0f;
	float V_9 = 0.0f;
	Enumerator_t785E35998B1974B111A16DAFC5F62FCDEC615835 V_10;
	memset((&V_10), 0, sizeof(V_10));
	Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* V_11 = NULL;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_12;
	memset((&V_12), 0, sizeof(V_12));
	RuntimeObject* V_13 = NULL;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_14;
	memset((&V_14), 0, sizeof(V_14));
	float V_15 = 0.0f;
	float V_16 = 0.0f;
	float V_17 = 0.0f;
	{
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&V_0), (1.0E+09f), (1.0E+09f), (1.0E+09f), NULL);
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&V_1), (-1.0E+09f), (-1.0E+09f), (-1.0E+09f), NULL);
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_0 = ___0_scene;
		NullCheck(L_0);
		Enumerator_t785E35998B1974B111A16DAFC5F62FCDEC615835 L_1;
		L_1 = List_1_GetEnumerator_mCD2F70F8886ECEF58C2B0ACD4CC85F99A80733B7(L_0, List_1_GetEnumerator_mCD2F70F8886ECEF58C2B0ACD4CC85F99A80733B7_RuntimeMethod_var);
		V_10 = L_1;
	}
	{
		auto __finallyBlock = il2cpp::utils::Finally([&]
		{

FINALLY_00a4:
			{
				Enumerator_Dispose_mF1558E7B7B84D786841C7B736ECC77BEED4B1367((&V_10), Enumerator_Dispose_mF1558E7B7B84D786841C7B736ECC77BEED4B1367_RuntimeMethod_var);
				return;
			}
		});
		try
		{
			{
				goto IL_0099_1;
			}

IL_0036_1:
			{
				Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_2;
				L_2 = Enumerator_get_Current_m7673828665C413CFC1943B4A6DB0F0148D99596B_inline((&V_10), Enumerator_get_Current_m7673828665C413CFC1943B4A6DB0F0148D99596B_RuntimeMethod_var);
				V_11 = L_2;
				V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_3 = V_0;
				Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_4 = V_11;
				NullCheck(L_4);
				V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_5 = L_4->___Center;
				Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_6 = V_11;
				NullCheck(L_6);
				float L_7 = L_6->___Radius;
				Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_8 = V_11;
				NullCheck(L_8);
				float L_9 = L_8->___Radius;
				Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_10 = V_11;
				NullCheck(L_10);
				float L_11 = L_10->___Radius;
				V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12;
				memset((&L_12), 0, sizeof(L_12));
				V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_12), L_7, L_9, L_11, NULL);
				V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_13;
				L_13 = V3_op_Subtraction_mBF754B8B036EFB8C8B32E2C6A9799AFC1B245482(L_5, L_12, NULL);
				V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_14;
				L_14 = V3_Min_mD303066D8BA6D62D502E1D8BD1B22E60AD952D53(L_3, L_13, NULL);
				V_0 = L_14;
				V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_15 = V_1;
				Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_16 = V_11;
				NullCheck(L_16);
				V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_17 = L_16->___Center;
				Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_18 = V_11;
				NullCheck(L_18);
				float L_19 = L_18->___Radius;
				Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_20 = V_11;
				NullCheck(L_20);
				float L_21 = L_20->___Radius;
				Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_22 = V_11;
				NullCheck(L_22);
				float L_23 = L_22->___Radius;
				V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_24;
				memset((&L_24), 0, sizeof(L_24));
				V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_24), L_19, L_21, L_23, NULL);
				V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_25;
				L_25 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_17, L_24, NULL);
				V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_26;
				L_26 = V3_Max_m0E6A19FFD4652EF0A8D51250B6A5D96A07E08925(L_15, L_25, NULL);
				V_1 = L_26;
			}

IL_0099_1:
			{
				bool L_27;
				L_27 = Enumerator_MoveNext_mD22EC89FDB16CDABC6AB475A8E71C56858023AC5((&V_10), Enumerator_MoveNext_mD22EC89FDB16CDABC6AB475A8E71C56858023AC5_RuntimeMethod_var);
				if (L_27)
				{
					goto IL_0036_1;
				}
			}
			{
				goto IL_00b2;
			}
		}
		catch(Il2CppExceptionWrapper& e)
		{
			__finallyBlock.StoreException(e.ex);
		}
	}

IL_00b2:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_28 = V_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_29 = V_1;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_30;
		L_30 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_28, L_29, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_31;
		L_31 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_30, (0.5f), NULL);
		V_2 = L_31;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_32 = V_1;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_33 = V_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_34;
		L_34 = V3_op_Subtraction_mBF754B8B036EFB8C8B32E2C6A9799AFC1B245482(L_32, L_33, NULL);
		V_12 = L_34;
		float L_35;
		L_35 = V3_get_Length_mE87BCCE258FFBA7CC979830CD4B263B2BD63B9DE((&V_12), NULL);
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_36;
		L_36 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA((0.100000001f), ((float)il2cpp_codegen_multiply(L_35, (0.5f))), NULL);
		V_3 = L_36;
		il2cpp_codegen_initobj((&V_4), sizeof(Cam_tEC8E349D27505A9C38386DDE6496344928DCC379));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_37 = V_2;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_38 = ((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___ViewDir;
		float L_39 = V_3;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_40;
		L_40 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_38, ((float)il2cpp_codegen_multiply(L_39, (4.19999981f))), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_41;
		L_41 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_37, L_40, NULL);
		(&V_4)->___Eye = L_41;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_42 = V_2;
		Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_43 = V_4;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_44 = L_43.___Eye;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_45;
		L_45 = V3_op_Subtraction_mBF754B8B036EFB8C8B32E2C6A9799AFC1B245482(L_42, L_44, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_46;
		L_46 = V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892(L_45, NULL);
		(&V_4)->___F = L_46;
		Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_47 = V_4;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_48 = L_47.___F;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_49;
		memset((&L_49), 0, sizeof(L_49));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_49), (0.0f), (1.0f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_50;
		L_50 = V3_Cross_m88C432D75F76D3BCA973DF38C923E355B02048F3(L_48, L_49, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_51;
		L_51 = V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892(L_50, NULL);
		(&V_4)->___R = L_51;
		Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_52 = V_4;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_53 = L_52.___R;
		Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_54 = V_4;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_55 = L_54.___F;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_56;
		L_56 = V3_Cross_m88C432D75F76D3BCA973DF38C923E355B02048F3(L_53, L_55, NULL);
		(&V_4)->___U = L_56;
		V_5 = (1.0E+09f);
		V_6 = (-1.0E+09f);
		V_7 = (1.0E+09f);
		V_8 = (-1.0E+09f);
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_57 = ___0_scene;
		NullCheck(L_57);
		Enumerator_t785E35998B1974B111A16DAFC5F62FCDEC615835 L_58;
		L_58 = List_1_GetEnumerator_mCD2F70F8886ECEF58C2B0ACD4CC85F99A80733B7(L_57, List_1_GetEnumerator_mCD2F70F8886ECEF58C2B0ACD4CC85F99A80733B7_RuntimeMethod_var);
		V_10 = L_58;
	}
	{
		auto __finallyBlock = il2cpp::utils::Finally([&]
		{

FINALLY_0253:
			{
				Enumerator_Dispose_mF1558E7B7B84D786841C7B736ECC77BEED4B1367((&V_10), Enumerator_Dispose_mF1558E7B7B84D786841C7B736ECC77BEED4B1367_RuntimeMethod_var);
				return;
			}
		});
		try
		{
			{
				goto IL_0245_1;
			}

IL_0193_1:
			{
				Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_59;
				L_59 = Enumerator_get_Current_m7673828665C413CFC1943B4A6DB0F0148D99596B_inline((&V_10), Enumerator_get_Current_m7673828665C413CFC1943B4A6DB0F0148D99596B_RuntimeMethod_var);
				NullCheck(L_59);
				RuntimeObject* L_60;
				L_60 = Prim_Corners_m512FD0FEA6F7C961CB55E658A3D7053C4700C542(L_59, NULL);
				NullCheck(L_60);
				RuntimeObject* L_61;
				L_61 = InterfaceFuncInvoker0< RuntimeObject* >::Invoke(0, IEnumerable_1_t0AA6705B9378A4033A24C2FB0A50C35A32204416_il2cpp_TypeInfo_var, L_60);
				V_13 = L_61;
			}
			{
				auto __finallyBlock = il2cpp::utils::Finally([&]
				{

FINALLY_0239_1:
					{
						{
							RuntimeObject* L_62 = V_13;
							if (!L_62)
							{
								goto IL_0244_1;
							}
						}
						{
							RuntimeObject* L_63 = V_13;
							NullCheck(L_63);
							InterfaceActionInvoker0::Invoke(0, IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var, L_63);
						}

IL_0244_1:
						{
							return;
						}
					}
				});
				try
				{
					{
						goto IL_022b_2;
					}

IL_01ab_2:
					{
						RuntimeObject* L_64 = V_13;
						NullCheck(L_64);
						V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_65;
						L_65 = InterfaceFuncInvoker0< V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D >::Invoke(0, IEnumerator_1_tD99202564DB6FD9B170E7069409E5370D6604F6B_il2cpp_TypeInfo_var, L_64);
						Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_66 = V_4;
						V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_67 = L_66.___Eye;
						V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_68;
						L_68 = V3_op_Subtraction_mBF754B8B036EFB8C8B32E2C6A9799AFC1B245482(L_65, L_67, NULL);
						V_14 = L_68;
						V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_69 = V_14;
						Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_70 = V_4;
						V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_71 = L_70.___F;
						float L_72;
						L_72 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_69, L_71, NULL);
						V_15 = L_72;
						float L_73 = V_15;
						if ((((float)L_73) < ((float)(0.00100000005f))))
						{
							goto IL_022b_2;
						}
					}
					{
						V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_74 = V_14;
						Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_75 = V_4;
						V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_76 = L_75.___R;
						float L_77;
						L_77 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_74, L_76, NULL);
						float L_78 = V_15;
						V_16 = ((float)(L_77/L_78));
						V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_79 = V_14;
						Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_80 = V_4;
						V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_81 = L_80.___U;
						float L_82;
						L_82 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_79, L_81, NULL);
						float L_83 = V_15;
						V_17 = ((float)(L_82/L_83));
						float L_84 = V_5;
						float L_85 = V_16;
						il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
						float L_86;
						L_86 = Math_Min_mE913811A2F7566294BF4649A434282634E7254B3(L_84, L_85, NULL);
						V_5 = L_86;
						float L_87 = V_6;
						float L_88 = V_16;
						float L_89;
						L_89 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA(L_87, L_88, NULL);
						V_6 = L_89;
						float L_90 = V_7;
						float L_91 = V_17;
						float L_92;
						L_92 = Math_Min_mE913811A2F7566294BF4649A434282634E7254B3(L_90, L_91, NULL);
						V_7 = L_92;
						float L_93 = V_8;
						float L_94 = V_17;
						float L_95;
						L_95 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA(L_93, L_94, NULL);
						V_8 = L_95;
					}

IL_022b_2:
					{
						RuntimeObject* L_96 = V_13;
						NullCheck(L_96);
						bool L_97;
						L_97 = InterfaceFuncInvoker0< bool >::Invoke(0, IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var, L_96);
						if (L_97)
						{
							goto IL_01ab_2;
						}
					}
					{
						goto IL_0245_1;
					}
				}
				catch(Il2CppExceptionWrapper& e)
				{
					__finallyBlock.StoreException(e.ex);
				}
			}

IL_0245_1:
			{
				bool L_98;
				L_98 = Enumerator_MoveNext_mD22EC89FDB16CDABC6AB475A8E71C56858023AC5((&V_10), Enumerator_MoveNext_mD22EC89FDB16CDABC6AB475A8E71C56858023AC5_RuntimeMethod_var);
				if (L_98)
				{
					goto IL_0193_1;
				}
			}
			{
				goto IL_0261;
			}
		}
		catch(Il2CppExceptionWrapper& e)
		{
			__finallyBlock.StoreException(e.ex);
		}
	}

IL_0261:
	{
		float L_99 = V_6;
		float L_100 = V_5;
		float L_101 = V_8;
		float L_102 = V_7;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_103;
		L_103 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA(((float)il2cpp_codegen_subtract(L_99, L_100)), ((float)il2cpp_codegen_subtract(L_101, L_102)), NULL);
		V_9 = ((float)(L_103/(0.920000017f)));
		float L_104 = V_9;
		(&V_4)->___Span = L_104;
		float L_105 = V_5;
		float L_106 = V_6;
		float L_107 = V_9;
		(&V_4)->___X0 = ((float)il2cpp_codegen_subtract(((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_add(L_105, L_106)), (0.5f))), ((float)il2cpp_codegen_multiply(L_107, (0.5f)))));
		float L_108 = V_7;
		float L_109 = V_8;
		float L_110 = V_9;
		(&V_4)->___Y0 = ((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_add(L_108, L_109)), (0.5f))), ((float)il2cpp_codegen_multiply(L_110, (0.5f)))));
		Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_111 = V_4;
		return L_111;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F PayloadRender_Sample_mE7A4F8364A54709D1A58A600D4D029ED4B123C4A (List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* ___0_scene, Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 ___1_cam, float ___2_u, float ___3_v, int32_t ___4_backdrop, int32_t* ___5_id, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	float V_1 = 0.0f;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_2;
	memset((&V_2), 0, sizeof(V_2));
	Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 V_3;
	memset((&V_3), 0, sizeof(V_3));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_4;
	memset((&V_4), 0, sizeof(V_4));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_5;
	memset((&V_5), 0, sizeof(V_5));
	C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F V_6;
	memset((&V_6), 0, sizeof(V_6));
	{
		Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_0 = ___1_cam;
		float L_1 = L_0.___X0;
		float L_2 = ___2_u;
		Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_3 = ___1_cam;
		float L_4 = L_3.___Span;
		V_0 = ((float)il2cpp_codegen_add(L_1, ((float)il2cpp_codegen_multiply(L_2, L_4))));
		Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_5 = ___1_cam;
		float L_6 = L_5.___Y0;
		float L_7 = ___3_v;
		Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_8 = ___1_cam;
		float L_9 = L_8.___Span;
		V_1 = ((float)il2cpp_codegen_subtract(L_6, ((float)il2cpp_codegen_multiply(L_7, L_9))));
		Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_10 = ___1_cam;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_11 = L_10.___F;
		Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_12 = ___1_cam;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_13 = L_12.___R;
		float L_14 = V_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_15;
		L_15 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_13, L_14, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_16;
		L_16 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_11, L_15, NULL);
		Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_17 = ___1_cam;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_18 = L_17.___U;
		float L_19 = V_1;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_20;
		L_20 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_18, L_19, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_21;
		L_21 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_16, L_20, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_22;
		L_22 = V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892(L_21, NULL);
		V_2 = L_22;
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_23 = ___0_scene;
		Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_24 = ___1_cam;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_25 = L_24.___Eye;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_26 = V_2;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		bool L_27;
		L_27 = PayloadRender_Trace_m537188514DC0B279D080B22F51DB8A5234E6FD59(L_23, L_25, L_26, (1.0E+09f), (-1), (&V_3), NULL);
		if (L_27)
		{
			goto IL_00c3;
		}
	}
	{
		int32_t* L_28 = ___5_id;
		*((int32_t*)L_28) = (int32_t)(-1);
		int32_t L_29 = ___4_backdrop;
		if ((!(((uint32_t)L_29) == ((uint32_t)1))))
		{
			goto IL_0079;
		}
	}
	{
		il2cpp_codegen_initobj((&V_6), sizeof(C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F));
		C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F L_30 = V_6;
		return L_30;
	}

IL_0079:
	{
		float L_31 = ___2_u;
		float L_32 = ___3_v;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_33;
		L_33 = PayloadRender_Backdrop_mFF5B7FEABCD4EB45875518068136093EB183F09D(L_31, L_32, NULL);
		V_5 = L_33;
		il2cpp_codegen_initobj((&V_6), sizeof(C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_34 = V_5;
		float L_35 = L_34.___X;
		(&V_6)->___R = L_35;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_36 = V_5;
		float L_37 = L_36.___Y;
		(&V_6)->___G = L_37;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_38 = V_5;
		float L_39 = L_38.___Z;
		(&V_6)->___B = L_39;
		(&V_6)->___A = (1.0f);
		C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F L_40 = V_6;
		return L_40;
	}

IL_00c3:
	{
		int32_t* L_41 = ___5_id;
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 L_42 = V_3;
		int32_t L_43 = L_42.___Index;
		*((int32_t*)L_41) = (int32_t)L_43;
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_44 = ___0_scene;
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 L_45 = V_3;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_46 = V_2;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_47;
		L_47 = PayloadRender_Shade_m6993732C766049B3B2E5703DB6C41C29238C3975(L_44, L_45, L_46, NULL);
		V_4 = L_47;
		il2cpp_codegen_initobj((&V_6), sizeof(C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_48 = V_4;
		float L_49 = L_48.___X;
		(&V_6)->___R = L_49;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_50 = V_4;
		float L_51 = L_50.___Y;
		(&V_6)->___G = L_51;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_52 = V_4;
		float L_53 = L_52.___Z;
		(&V_6)->___B = L_53;
		(&V_6)->___A = (1.0f);
		C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F L_54 = V_6;
		return L_54;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D PayloadRender_Backdrop_mFF5B7FEABCD4EB45875518068136093EB183F09D (float ___0_u, float ___1_v, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	float V_1 = 0.0f;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_2;
	memset((&V_2), 0, sizeof(V_2));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_3;
	memset((&V_3), 0, sizeof(V_3));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_4;
	memset((&V_4), 0, sizeof(V_4));
	float V_5 = 0.0f;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D G_B5_0;
	memset((&G_B5_0), 0, sizeof(G_B5_0));
	{
		float L_0 = ___0_u;
		float L_1 = ___1_v;
		V_0 = ((float)il2cpp_codegen_subtract(L_1, (0.439999998f)));
		float L_2 = ((float)il2cpp_codegen_subtract(L_0, (0.5f)));
		float L_3 = V_0;
		float L_4 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_5;
		L_5 = sqrt(((double)((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(L_2, L_2)), ((float)il2cpp_codegen_multiply(L_3, L_4))))));
		V_1 = ((float)(((float)L_5)/(0.720000029f)));
		float L_6 = V_1;
		if ((!(((float)L_6) > ((float)(1.0f)))))
		{
			goto IL_0031;
		}
	}
	{
		V_1 = (1.0f);
	}

IL_0031:
	{
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&V_2), (0.0196078438f), (0.0313725509f), (0.101960786f), NULL);
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&V_3), (0.0588235296f), (0.0901960805f), (0.250980407f), NULL);
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&V_4), (0.141176477f), (0.200000003f), (0.447058827f), NULL);
		float L_7 = V_1;
		if ((((float)L_7) < ((float)(0.449999988f))))
		{
			goto IL_0091;
		}
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8 = V_3;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_9 = V_2;
		float L_10 = V_1;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_11;
		L_11 = V3_Lerp_mB6067E0973AEFE35323954D146ED580F05E2D125(L_8, L_9, ((float)(((float)il2cpp_codegen_subtract(L_10, (0.449999988f)))/(0.550000012f))), NULL);
		G_B5_0 = L_11;
		goto IL_00a0;
	}

IL_0091:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12 = V_4;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_13 = V_3;
		float L_14 = V_1;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_15;
		L_15 = V3_Lerp_mB6067E0973AEFE35323954D146ED580F05E2D125(L_12, L_13, ((float)(L_14/(0.449999988f))), NULL);
		G_B5_0 = L_15;
	}

IL_00a0:
	{
		float L_16 = ___1_v;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		float L_17;
		L_17 = PayloadRender_Smooth_m5380759CCAE4BC8D1592EBE7088C4AE973E5E9BE((0.779999971f), (1.0f), L_16, NULL);
		V_5 = ((float)il2cpp_codegen_multiply(L_17, (0.219999999f)));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_18;
		memset((&L_18), 0, sizeof(L_18));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_18), (0.227450982f), (0.164705887f), (0.368627459f), NULL);
		float L_19 = V_5;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_20;
		L_20 = V3_Lerp_mB6067E0973AEFE35323954D146ED580F05E2D125(G_B5_0, L_18, L_19, NULL);
		return L_20;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D PayloadRender_Shade_m6993732C766049B3B2E5703DB6C41C29238C3975 (List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* ___0_scene, Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 ___1_h, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___2_dir, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_get_Item_m658740103DB2DE2E2FF16DEFD463120C34BBE346_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_0;
	memset((&V_0), 0, sizeof(V_0));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_1;
	memset((&V_1), 0, sizeof(V_1));
	Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 V_2;
	memset((&V_2), 0, sizeof(V_2));
	float V_3 = 0.0f;
	float V_4 = 0.0f;
	float V_5 = 0.0f;
	float V_6 = 0.0f;
	float V_7 = 0.0f;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_8;
	memset((&V_8), 0, sizeof(V_8));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_9;
	memset((&V_9), 0, sizeof(V_9));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_10;
	memset((&V_10), 0, sizeof(V_10));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_11;
	memset((&V_11), 0, sizeof(V_11));
	float V_12 = 0.0f;
	float V_13 = 0.0f;
	float V_14 = 0.0f;
	Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 V_15;
	memset((&V_15), 0, sizeof(V_15));
	float V_16 = 0.0f;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_17;
	memset((&V_17), 0, sizeof(V_17));
	float V_18 = 0.0f;
	Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* G_B2_0 = NULL;
	Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* G_B1_0 = NULL;
	{
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_0 = ___0_scene;
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 L_1 = ___1_h;
		int32_t L_2 = L_1.___Index;
		NullCheck(L_0);
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_3;
		L_3 = List_1_get_Item_m658740103DB2DE2E2FF16DEFD463120C34BBE346(L_0, L_2, List_1_get_Item_m658740103DB2DE2E2FF16DEFD463120C34BBE346_RuntimeMethod_var);
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 L_4 = ___1_h;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_5 = L_4.___N;
		V_0 = L_5;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6 = ___2_dir;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_7;
		L_7 = V3_op_UnaryNegation_mC104632D789B94979F0AAE197F1A89BDC3773D14(L_6, NULL);
		V_1 = L_7;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8 = V_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_9 = V_1;
		float L_10;
		L_10 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_8, L_9, NULL);
		if ((!(((float)L_10) < ((float)(0.0f)))))
		{
			G_B2_0 = L_3;
			goto IL_002f;
		}
		G_B1_0 = L_3;
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_11 = V_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12;
		L_12 = V3_op_UnaryNegation_mC104632D789B94979F0AAE197F1A89BDC3773D14(L_11, NULL);
		V_0 = L_12;
		G_B2_0 = G_B1_0;
	}

IL_002f:
	{
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 L_13 = ___1_h;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_14;
		L_14 = PayloadRender_Surface_m0E4A5D0B544C846124A7422DED6E452C4E21D72D(G_B2_0, L_13, (&V_0), NULL);
		V_2 = L_14;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_15 = V_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_16 = V_1;
		float L_17;
		L_17 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_15, L_16, NULL);
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_18;
		L_18 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA((0.0f), L_17, NULL);
		V_3 = L_18;
		V_4 = (1.0f);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_19 = V_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_20 = ((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___KeyDir;
		float L_21;
		L_21 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_19, L_20, NULL);
		if ((!(((float)L_21) > ((float)(0.0f)))))
		{
			goto IL_0095;
		}
	}
	{
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_22 = ___0_scene;
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 L_23 = ___1_h;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_24 = L_23.___P;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_25 = V_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_26;
		L_26 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_25, (0.00400000019f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_27;
		L_27 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_24, L_26, NULL);
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_28 = ((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___KeyDir;
		bool L_29;
		L_29 = PayloadRender_Trace_m537188514DC0B279D080B22F51DB8A5234E6FD59(L_22, L_27, L_28, (1.0E+09f), (-1), (&V_15), NULL);
		if (!L_29)
		{
			goto IL_0095;
		}
	}
	{
		V_4 = (0.180000007f);
	}

IL_0095:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_30 = V_0;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_31 = ((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___KeyDir;
		float L_32;
		L_32 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_30, L_31, NULL);
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_33;
		L_33 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA((0.0f), L_32, NULL);
		float L_34 = V_4;
		V_5 = ((float)il2cpp_codegen_multiply(L_33, L_34));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_35 = V_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_36 = ((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___RimDir;
		float L_37;
		L_37 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_35, L_36, NULL);
		float L_38;
		L_38 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA((0.0f), L_37, NULL);
		V_6 = L_38;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_39 = V_0;
		float L_40 = L_39.___Y;
		V_7 = ((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(L_40, (0.5f))), (0.5f)));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_41;
		memset((&L_41), 0, sizeof(L_41));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_41), (0.129999995f), (0.0799999982f), (0.0700000003f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_42;
		memset((&L_42), 0, sizeof(L_42));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_42), (0.119999997f), (0.150000006f), (0.280000001f), NULL);
		float L_43 = V_7;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_44;
		L_44 = V3_Lerp_mB6067E0973AEFE35323954D146ED580F05E2D125(L_41, L_42, L_43, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_45 = ((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___KeyCol;
		float L_46 = V_5;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_47;
		L_47 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_45, L_46, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_48;
		L_48 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_44, L_47, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_49 = ((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___RimCol;
		float L_50 = V_6;
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_51 = V_2;
		float L_52 = L_51.___Metal;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_53;
		L_53 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_49, ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_multiply(L_50, (0.550000012f))), ((float)il2cpp_codegen_subtract((1.0f), ((float)il2cpp_codegen_multiply((0.5f), L_52)))))), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_54;
		L_54 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_48, L_53, NULL);
		V_8 = L_54;
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_55 = V_2;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_56 = L_55.___Albedo;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_57 = V_8;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_58;
		L_58 = V3_Mul_mF80A0EED832B6F3269918C7C10FA91E36C56F5B8(L_56, L_57, NULL);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_59 = V_2;
		float L_60 = L_59.___Diffuse;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_61;
		L_61 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_58, L_60, NULL);
		V_9 = L_61;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_62 = ((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___KeyDir;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_63 = V_1;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_64;
		L_64 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_62, L_63, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_65;
		L_65 = V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892(L_64, NULL);
		V_10 = L_65;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_66 = ((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___RimDir;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_67 = V_1;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_68;
		L_68 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_66, L_67, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_69;
		L_69 = V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892(L_68, NULL);
		V_11 = L_69;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_70 = V_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_71 = V_10;
		float L_72;
		L_72 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_70, L_71, NULL);
		float L_73;
		L_73 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA((0.0f), L_72, NULL);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_74 = V_2;
		float L_75 = L_74.___Power;
		float L_76;
		L_76 = PayloadRender_Pow_mA707E79B5D2EC2D61714F837291FBD2BFA7AEEA0(L_73, L_75, NULL);
		float L_77 = V_4;
		V_12 = ((float)il2cpp_codegen_multiply(L_76, L_77));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_78 = V_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_79 = V_11;
		float L_80;
		L_80 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_78, L_79, NULL);
		float L_81;
		L_81 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA((0.0f), L_80, NULL);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_82 = V_2;
		float L_83 = L_82.___Power;
		float L_84;
		L_84 = PayloadRender_Pow_mA707E79B5D2EC2D61714F837291FBD2BFA7AEEA0(L_81, L_83, NULL);
		V_13 = ((float)il2cpp_codegen_multiply(L_84, (0.600000024f)));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_85 = V_9;
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_86 = V_2;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_87 = L_86.___Spec;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_88 = ((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___KeyCol;
		float L_89 = V_12;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_90;
		L_90 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_88, L_89, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_91 = ((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___RimCol;
		float L_92 = V_13;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_93;
		L_93 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_91, L_92, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_94;
		L_94 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_90, L_93, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_95;
		L_95 = V3_Mul_mF80A0EED832B6F3269918C7C10FA91E36C56F5B8(L_87, L_94, NULL);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_96 = V_2;
		float L_97 = L_96.___SpecK;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_98;
		L_98 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_95, L_97, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_99;
		L_99 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_85, L_98, NULL);
		V_9 = L_99;
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_100 = V_2;
		float L_101 = L_100.___Env;
		if ((!(((float)L_101) > ((float)(0.0f)))))
		{
			goto IL_02b3;
		}
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_102 = ___2_dir;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_103 = V_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_104 = ___2_dir;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_105 = V_0;
		float L_106;
		L_106 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_104, L_105, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_107;
		L_107 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_103, ((float)il2cpp_codegen_multiply((2.0f), L_106)), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_108;
		L_108 = V3_op_Subtraction_mBF754B8B036EFB8C8B32E2C6A9799AFC1B245482(L_102, L_107, NULL);
		float L_109 = V_3;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		float L_110;
		L_110 = PayloadRender_Pow_mA707E79B5D2EC2D61714F837291FBD2BFA7AEEA0(((float)il2cpp_codegen_subtract((1.0f), L_109)), (4.0f), NULL);
		V_16 = ((float)il2cpp_codegen_add((0.349999994f), ((float)il2cpp_codegen_multiply((0.649999976f), L_110))));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_111;
		L_111 = PayloadRender_Env_m179AA6C5AB885643D562AD08FEE47EBB2F85A21A(L_108, NULL);
		V_17 = L_111;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_112 = V_17;
		float L_113 = L_112.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_114 = V_17;
		float L_115 = L_114.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_116 = V_17;
		float L_117 = L_116.___Z;
		V_18 = ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_add(((float)il2cpp_codegen_add(L_113, L_115)), L_117)), (0.333333343f)));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_118 = V_17;
		float L_119 = V_18;
		float L_120 = V_18;
		float L_121 = V_18;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_122;
		memset((&L_122), 0, sizeof(L_122));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_122), L_119, L_120, L_121, NULL);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_123 = V_2;
		float L_124 = L_123.___Metal;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_125;
		L_125 = V3_Lerp_mB6067E0973AEFE35323954D146ED580F05E2D125(L_118, L_122, L_124, NULL);
		V_17 = L_125;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_126 = V_9;
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_127 = V_2;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_128 = L_127.___Spec;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_129 = V_17;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_130;
		L_130 = V3_Mul_mF80A0EED832B6F3269918C7C10FA91E36C56F5B8(L_128, L_129, NULL);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_131 = V_2;
		float L_132 = L_131.___Env;
		float L_133 = V_16;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_134;
		L_134 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_130, ((float)il2cpp_codegen_multiply(L_132, L_133)), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_135;
		L_135 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_126, L_134, NULL);
		V_9 = L_135;
	}

IL_02b3:
	{
		float L_136 = V_3;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		float L_137;
		L_137 = PayloadRender_Pow_mA707E79B5D2EC2D61714F837291FBD2BFA7AEEA0(((float)il2cpp_codegen_subtract((1.0f), L_136)), (3.0f), NULL);
		V_14 = L_137;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_138 = V_9;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_139 = ((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___RimCol;
		float L_140 = V_14;
		float L_141 = V_6;
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_142 = V_2;
		float L_143 = L_142.___Metal;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_144;
		L_144 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_139, ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_multiply(L_140, (0.280000001f))), ((float)il2cpp_codegen_add((0.400000006f), ((float)il2cpp_codegen_multiply((0.600000024f), L_141)))))), ((float)il2cpp_codegen_subtract((1.0f), ((float)il2cpp_codegen_multiply((0.600000024f), L_143)))))), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_145;
		L_145 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_138, L_144, NULL);
		V_9 = L_145;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_146 = V_9;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_147;
		L_147 = PayloadRender_Tone_mC05451DA4C9A7A040DA88C66AE8AEA98A9D7D66A(L_146, NULL);
		return L_147;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D PayloadRender_Env_m179AA6C5AB885643D562AD08FEE47EBB2F85A21A (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_r, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	float V_1 = 0.0f;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_2;
	memset((&V_2), 0, sizeof(V_2));
	float V_3 = 0.0f;
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = ___0_r;
		float L_1 = L_0.___Y;
		if ((!(((float)L_1) >= ((float)(0.0f)))))
		{
			goto IL_0099;
		}
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2 = ___0_r;
		float L_3 = L_2.___Y;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_4;
		L_4 = Math_Min_mE913811A2F7566294BF4649A434282634E7254B3((1.0f), L_3, NULL);
		double L_5;
		L_5 = sqrt(((double)L_4));
		V_1 = ((float)L_5);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6;
		memset((&L_6), 0, sizeof(L_6));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_6), (0.949999988f), (0.579999983f), (0.319999993f), NULL);
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&V_2), (0.159999996f), (0.219999999f), (0.519999981f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_7 = V_2;
		float L_8 = V_1;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_9;
		L_9 = V3_Lerp_mB6067E0973AEFE35323954D146ED580F05E2D125(L_6, L_7, L_8, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10 = ___0_r;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_11 = ((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___KeyDir;
		float L_12;
		L_12 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_10, L_11, NULL);
		float L_13;
		L_13 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA((0.0f), L_12, NULL);
		float L_14;
		L_14 = PayloadRender_Pow_mA707E79B5D2EC2D61714F837291FBD2BFA7AEEA0(L_13, (24.0f), NULL);
		V_3 = L_14;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_15;
		memset((&L_15), 0, sizeof(L_15));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_15), (1.39999998f), (1.25f), (1.0f), NULL);
		float L_16 = V_3;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_17;
		L_17 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_15, L_16, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_18;
		L_18 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_9, L_17, NULL);
		return L_18;
	}

IL_0099:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_19 = ___0_r;
		float L_20 = L_19.___Y;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_21;
		L_21 = Math_Min_mE913811A2F7566294BF4649A434282634E7254B3((1.0f), ((float)il2cpp_codegen_multiply(((-L_20)), (2.0f))), NULL);
		V_0 = L_21;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_22;
		memset((&L_22), 0, sizeof(L_22));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_22), (0.620000005f), (0.379999995f), (0.25999999f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_23;
		memset((&L_23), 0, sizeof(L_23));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_23), (0.159999996f), (0.119999997f), (0.159999996f), NULL);
		float L_24 = V_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_25;
		L_25 = V3_Lerp_mB6067E0973AEFE35323954D146ED580F05E2D125(L_22, L_23, L_24, NULL);
		return L_25;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D PayloadRender_Tone_mC05451DA4C9A7A040DA88C66AE8AEA98A9D7D66A (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_c, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = ___0_c;
		float L_1 = L_0.___X;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		float L_2;
		L_2 = PayloadRender_Filmic_mCA6AD448A9DA9D3EEDCAFFFAD769D171A2B1B782(L_1, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_3 = ___0_c;
		float L_4 = L_3.___Y;
		float L_5;
		L_5 = PayloadRender_Filmic_mCA6AD448A9DA9D3EEDCAFFFAD769D171A2B1B782(L_4, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6 = ___0_c;
		float L_7 = L_6.___Z;
		float L_8;
		L_8 = PayloadRender_Filmic_mCA6AD448A9DA9D3EEDCAFFFAD769D171A2B1B782(L_7, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_9;
		memset((&L_9), 0, sizeof(L_9));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_9), L_2, L_5, L_8, NULL);
		return L_9;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadRender_Filmic_mCA6AD448A9DA9D3EEDCAFFFAD769D171A2B1B782 (float ___0_x, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		float L_0 = ___0_x;
		if ((!(((float)L_0) <= ((float)(0.0f)))))
		{
			goto IL_000e;
		}
	}
	{
		return (0.0f);
	}

IL_000e:
	{
		float L_1 = ___0_x;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_2;
		L_2 = exp(((double)((float)il2cpp_codegen_multiply(((-L_1)), (1.25f)))));
		double L_3;
		L_3 = Math_Pow_mEAE651F0858203FBE12B72B6A53951BBD0FB5265(((double)((float)il2cpp_codegen_subtract((1.0f), ((float)L_2)))), (0.8928571343421936), NULL);
		return ((float)L_3);
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 PayloadRender_M_m9400B16719658CD34FD3CFA82FA9E8C75FD60D94 (float ___0_r, float ___1_g, float ___2_b, float ___3_diffuse, float ___4_sr, float ___5_sg, float ___6_sb, float ___7_specK, float ___8_power, float ___9_env, const RuntimeMethod* method) 
{
	Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 V_0;
	memset((&V_0), 0, sizeof(V_0));
	{
		il2cpp_codegen_initobj((&V_0), sizeof(Mat_tF560921B2617E9F9BAF232386519B4016C462CA1));
		float L_0 = ___0_r;
		float L_1 = ___1_g;
		float L_2 = ___2_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_3;
		memset((&L_3), 0, sizeof(L_3));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_3), L_0, L_1, L_2, NULL);
		(&V_0)->___Albedo = L_3;
		float L_4 = ___3_diffuse;
		(&V_0)->___Diffuse = L_4;
		float L_5 = ___4_sr;
		float L_6 = ___5_sg;
		float L_7 = ___6_sb;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8;
		memset((&L_8), 0, sizeof(L_8));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_8), L_5, L_6, L_7, NULL);
		(&V_0)->___Spec = L_8;
		float L_9 = ___7_specK;
		(&V_0)->___SpecK = L_9;
		float L_10 = ___8_power;
		(&V_0)->___Power = L_10;
		float L_11 = ___9_env;
		(&V_0)->___Env = L_11;
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_12 = V_0;
		return L_12;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 PayloadRender_Surface_m0E4A5D0B544C846124A7422DED6E452C4E21D72D (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* ___0_p, Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 ___1_h, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* ___2_n, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_0;
	memset((&V_0), 0, sizeof(V_0));
	int32_t V_1 = 0;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_2;
	memset((&V_2), 0, sizeof(V_2));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_3;
	memset((&V_3), 0, sizeof(V_3));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_4;
	memset((&V_4), 0, sizeof(V_4));
	bool V_5 = false;
	float V_6 = 0.0f;
	float V_7 = 0.0f;
	Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 V_8;
	memset((&V_8), 0, sizeof(V_8));
	float V_9 = 0.0f;
	float V_10 = 0.0f;
	float V_11 = 0.0f;
	float V_12 = 0.0f;
	float V_13 = 0.0f;
	float V_14 = 0.0f;
	Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 V_15;
	memset((&V_15), 0, sizeof(V_15));
	Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 V_16;
	memset((&V_16), 0, sizeof(V_16));
	Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 V_17;
	memset((&V_17), 0, sizeof(V_17));
	Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 V_18;
	memset((&V_18), 0, sizeof(V_18));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D G_B4_0;
	memset((&G_B4_0), 0, sizeof(G_B4_0));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D G_B4_1;
	memset((&G_B4_1), 0, sizeof(G_B4_1));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* G_B4_2 = NULL;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D G_B3_0;
	memset((&G_B3_0), 0, sizeof(G_B3_0));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D G_B3_1;
	memset((&G_B3_1), 0, sizeof(G_B3_1));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* G_B3_2 = NULL;
	float G_B5_0 = 0.0f;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D G_B5_1;
	memset((&G_B5_1), 0, sizeof(G_B5_1));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D G_B5_2;
	memset((&G_B5_2), 0, sizeof(G_B5_2));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* G_B5_3 = NULL;
	Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 G_B8_0;
	memset((&G_B8_0), 0, sizeof(G_B8_0));
	int32_t G_B17_0 = 0;
	int32_t G_B19_0 = 0;
	int32_t G_B18_0 = 0;
	int32_t G_B20_0 = 0;
	int32_t G_B20_1 = 0;
	int32_t G_B22_0 = 0;
	int32_t G_B21_0 = 0;
	int32_t G_B24_0 = 0;
	int32_t G_B23_0 = 0;
	{
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 L_0 = ___1_h;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_1 = L_0.___Local;
		V_0 = L_1;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_2 = ___0_p;
		NullCheck(L_2);
		int32_t L_3 = L_2->___Surface;
		V_1 = L_3;
		int32_t L_4 = V_1;
		switch (L_4)
		{
			case 0:
			{
				goto IL_003d;
			}
			case 1:
			{
				goto IL_003d;
			}
			case 2:
			{
				goto IL_02ab;
			}
			case 3:
			{
				goto IL_02b4;
			}
			case 4:
			{
				goto IL_0434;
			}
			case 5:
			{
				goto IL_04a8;
			}
			case 6:
			{
				goto IL_04f0;
			}
			case 7:
			{
				goto IL_0528;
			}
			case 8:
			{
				goto IL_0560;
			}
		}
	}
	{
		goto IL_0598;
	}

IL_003d:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_5 = V_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6;
		L_6 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_5, (19.0f), NULL);
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_7 = ___0_p;
		NullCheck(L_7);
		float L_8 = L_7->___Seed;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_9 = ___0_p;
		NullCheck(L_9);
		float L_10 = L_9->___Seed;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_11 = ___0_p;
		NullCheck(L_11);
		float L_12 = L_11->___Seed;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_13;
		memset((&L_13), 0, sizeof(L_13));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_13), L_8, ((float)il2cpp_codegen_multiply(L_10, (0.699999988f))), ((float)il2cpp_codegen_multiply(L_12, (1.29999995f))), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_14;
		L_14 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_6, L_13, NULL);
		V_2 = L_14;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_15 = V_2;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		float L_16;
		L_16 = PayloadRender_Noise_mA392E35C9ECF60BDD72D5CCB2921218AA8E0A970(L_15, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_17 = V_2;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_18;
		memset((&L_18), 0, sizeof(L_18));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_18), (17.1000004f), (3.29999995f), (9.19999981f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_19;
		L_19 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_17, L_18, NULL);
		float L_20;
		L_20 = PayloadRender_Noise_mA392E35C9ECF60BDD72D5CCB2921218AA8E0A970(L_19, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_21 = V_2;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_22;
		memset((&L_22), 0, sizeof(L_22));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_22), (5.69999981f), (23.8999996f), (1.10000002f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_23;
		L_23 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_21, L_22, NULL);
		float L_24;
		L_24 = PayloadRender_Noise_mA392E35C9ECF60BDD72D5CCB2921218AA8E0A970(L_23, NULL);
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&V_3), ((float)il2cpp_codegen_subtract(L_16, (0.5f))), ((float)il2cpp_codegen_subtract(L_20, (0.5f))), ((float)il2cpp_codegen_subtract(L_24, (0.5f))), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_25 = V_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_26;
		L_26 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_25, (46.0f), NULL);
		V_4 = L_26;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_27 = V_3;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_28 = V_4;
		float L_29;
		L_29 = PayloadRender_Noise_mA392E35C9ECF60BDD72D5CCB2921218AA8E0A970(L_28, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_30 = V_4;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_31;
		memset((&L_31), 0, sizeof(L_31));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_31), (7.69999981f), (1.89999998f), (4.4000001f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_32;
		L_32 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_30, L_31, NULL);
		float L_33;
		L_33 = PayloadRender_Noise_mA392E35C9ECF60BDD72D5CCB2921218AA8E0A970(L_32, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_34 = V_4;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_35;
		memset((&L_35), 0, sizeof(L_35));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_35), (2.20000005f), (8.80000019f), (6.5999999f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_36;
		L_36 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_34, L_35, NULL);
		float L_37;
		L_37 = PayloadRender_Noise_mA392E35C9ECF60BDD72D5CCB2921218AA8E0A970(L_36, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_38;
		memset((&L_38), 0, sizeof(L_38));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_38), ((float)il2cpp_codegen_subtract(L_29, (0.5f))), ((float)il2cpp_codegen_subtract(L_33, (0.5f))), ((float)il2cpp_codegen_subtract(L_37, (0.5f))), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_39;
		L_39 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_38, (0.550000012f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_40;
		L_40 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_27, L_39, NULL);
		V_3 = L_40;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_41 = ___0_p;
		NullCheck(L_41);
		int32_t L_42 = L_41->___Surface;
		V_5 = (bool)((((int32_t)L_42) == ((int32_t)0))? 1 : 0);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_43 = ___2_n;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_44 = ___2_n;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_45 = (*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_44);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_46 = V_3;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_47 = ___2_n;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_48 = (*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_47);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_49 = V_3;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_50 = ___2_n;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_51 = (*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_50);
		float L_52;
		L_52 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_49, L_51, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_53;
		L_53 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_48, L_52, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_54;
		L_54 = V3_op_Subtraction_mBF754B8B036EFB8C8B32E2C6A9799AFC1B245482(L_46, L_53, NULL);
		bool L_55 = V_5;
		if (L_55)
		{
			G_B4_0 = L_54;
			G_B4_1 = L_45;
			G_B4_2 = L_43;
			goto IL_0184;
		}
		G_B3_0 = L_54;
		G_B3_1 = L_45;
		G_B3_2 = L_43;
	}
	{
		G_B5_0 = (0.449999988f);
		G_B5_1 = G_B3_0;
		G_B5_2 = G_B3_1;
		G_B5_3 = G_B3_2;
		goto IL_0189;
	}

IL_0184:
	{
		G_B5_0 = (0.850000024f);
		G_B5_1 = G_B4_0;
		G_B5_2 = G_B4_1;
		G_B5_3 = G_B4_2;
	}

IL_0189:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_56;
		L_56 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(G_B5_1, G_B5_0, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_57;
		L_57 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(G_B5_2, L_56, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_58;
		L_58 = V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892(L_57, NULL);
		*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)G_B5_3 = L_58;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_59 = V_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_60;
		L_60 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_59, (61.0f), NULL);
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		float L_61;
		L_61 = PayloadRender_Noise_mA392E35C9ECF60BDD72D5CCB2921218AA8E0A970(L_60, NULL);
		V_6 = ((float)il2cpp_codegen_add((0.879999995f), ((float)il2cpp_codegen_multiply((0.239999995f), L_61))));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_62 = V_0;
		float L_63;
		L_63 = PayloadRender_Stitch_m9496EAB56707B260EE63E06BE961539E8C301B97(L_62, (0.209999993f), (0.0120000001f), NULL);
		V_7 = L_63;
		bool L_64 = V_5;
		if (L_64)
		{
			goto IL_0213;
		}
	}
	{
		float L_65 = V_6;
		float L_66 = V_6;
		float L_67 = V_6;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_68;
		L_68 = PayloadRender_M_m9400B16719658CD34FD3CFA82FA9E8C75FD60D94(((float)il2cpp_codegen_multiply((0.839999974f), L_65)), ((float)il2cpp_codegen_multiply((0.860000014f), L_66)), ((float)il2cpp_codegen_multiply((0.899999976f), L_67)), (0.75f), (0.899999976f), (0.920000017f), (0.980000019f), (1.20000005f), (40.0f), (0.600000024f), NULL);
		G_B8_0 = L_68;
		goto IL_0253;
	}

IL_0213:
	{
		float L_69 = V_6;
		float L_70 = V_6;
		float L_71 = V_6;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_72;
		L_72 = PayloadRender_M_m9400B16719658CD34FD3CFA82FA9E8C75FD60D94(((float)il2cpp_codegen_multiply((0.980000019f), L_69)), ((float)il2cpp_codegen_multiply((0.639999986f), L_70)), ((float)il2cpp_codegen_multiply((0.140000001f), L_71)), (0.620000005f), (1.0f), (0.720000029f), (0.239999995f), (1.79999995f), (44.0f), (1.29999995f), NULL);
		G_B8_0 = L_72;
	}

IL_0253:
	{
		V_8 = G_B8_0;
		(&V_8)->___Metal = (0.850000024f);
		float L_73 = V_7;
		if ((!(((float)L_73) > ((float)(0.0f)))))
		{
			goto IL_02a8;
		}
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_74 = (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)(&(&V_8)->___Albedo);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_75 = L_74;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_76 = (*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_75);
		float L_77 = V_7;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_78;
		L_78 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_76, ((float)il2cpp_codegen_subtract((1.0f), ((float)il2cpp_codegen_multiply((0.349999994f), L_77)))), NULL);
		*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_75 = L_78;
		float* L_79 = (float*)(&(&V_8)->___Env);
		float* L_80 = L_79;
		float L_81 = *((float*)L_80);
		float L_82 = V_7;
		*((float*)L_80) = (float)((float)il2cpp_codegen_multiply(L_81, ((float)il2cpp_codegen_subtract((1.0f), ((float)il2cpp_codegen_multiply((0.449999988f), L_82))))));
	}

IL_02a8:
	{
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_83 = V_8;
		return L_83;
	}

IL_02ab:
	{
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_84 = ___0_p;
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 L_85 = ___1_h;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_86 = V_0;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_87;
		L_87 = PayloadRender_Cells_mE701F463E8E2334ECAA9C3A611CF03FA2C88042A(L_84, L_85, L_86, NULL);
		return L_87;
	}

IL_02b4:
	{
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_88 = ___0_p;
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 L_89 = ___1_h;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_90 = V_0;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		PayloadRender_FaceUv_m80043B41EF1004805E20B0FB665CF214947D3310(L_88, L_89, L_90, (&V_9), (&V_10), (&V_11), (&V_12), NULL);
		float L_91 = V_9;
		float L_92;
		L_92 = PayloadRender_Line_m585D11C93BA5AD4FEEC6323F2E1D6B9A5980B59F(L_91, (0.239999995f), (0.00700000022f), NULL);
		float L_93 = V_10;
		float L_94;
		L_94 = PayloadRender_Line_m585D11C93BA5AD4FEEC6323F2E1D6B9A5980B59F(L_93, (0.239999995f), (0.00700000022f), NULL);
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_95;
		L_95 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA(L_92, L_94, NULL);
		V_13 = L_95;
		float L_96 = V_9;
		float L_97;
		L_97 = PayloadRender_Frac_mCB86369BB619EE9BE740A60F04823BB2377D9BBF(((float)(L_96/(0.119999997f))), NULL);
		float L_98 = V_10;
		float L_99;
		L_99 = PayloadRender_Frac_mCB86369BB619EE9BE740A60F04823BB2377D9BBF(((float)(L_98/(0.119999997f))), NULL);
		V_14 = L_99;
		float L_100;
		L_100 = fabsf(((float)il2cpp_codegen_subtract(L_97, (0.5f))));
		if ((!(((float)L_100) < ((float)(0.0900000036f)))))
		{
			goto IL_0359;
		}
	}
	{
		float L_101 = V_14;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_102;
		L_102 = fabsf(((float)il2cpp_codegen_subtract(L_101, (0.5f))));
		if ((!(((float)L_102) < ((float)(0.0900000036f)))))
		{
			goto IL_0359;
		}
	}
	{
		float L_103 = V_9;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		float L_104;
		L_104 = PayloadRender_Frac_mCB86369BB619EE9BE740A60F04823BB2377D9BBF(((float)(L_103/(0.239999995f))), NULL);
		if ((!(((float)L_104) < ((float)(0.5f)))))
		{
			goto IL_0359;
		}
	}
	{
		float L_105 = V_10;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		float L_106;
		L_106 = PayloadRender_Frac_mCB86369BB619EE9BE740A60F04823BB2377D9BBF(((float)(L_105/(0.239999995f))), NULL);
		G_B17_0 = ((((float)L_106) < ((float)(0.5f)))? 1 : 0);
		goto IL_035a;
	}

IL_0359:
	{
		G_B17_0 = 0;
	}

IL_035a:
	{
		float L_107 = V_10;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_108;
		L_108 = fabsf(L_107);
		float L_109 = V_12;
		if ((!(((float)L_108) < ((float)((float)il2cpp_codegen_multiply(L_109, (0.319999993f)))))))
		{
			G_B19_0 = G_B17_0;
			goto IL_0385;
		}
		G_B18_0 = G_B17_0;
	}
	{
		float L_110 = V_9;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		float L_111;
		L_111 = PayloadRender_Line_m585D11C93BA5AD4FEEC6323F2E1D6B9A5980B59F(L_110, (0.0350000001f), (0.00400000019f), NULL);
		G_B20_0 = ((((float)L_111) > ((float)(0.0f)))? 1 : 0);
		G_B20_1 = G_B18_0;
		goto IL_0386;
	}

IL_0385:
	{
		G_B20_0 = 0;
		G_B20_1 = G_B19_0;
	}

IL_0386:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_112;
		L_112 = PayloadRender_M_m9400B16719658CD34FD3CFA82FA9E8C75FD60D94((0.819999993f), (0.819999993f), (0.800000012f), (1.0f), (0.899999976f), (0.899999976f), (0.899999976f), (0.349999994f), (22.0f), (0.180000007f), NULL);
		V_15 = L_112;
		if (!G_B20_0)
		{
			G_B22_0 = G_B20_1;
			goto IL_03e6;
		}
		G_B21_0 = G_B20_1;
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_113;
		memset((&L_113), 0, sizeof(L_113));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_113), (0.920000017f), (0.930000007f), (0.939999998f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_114;
		L_114 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_113, (0.899999976f), NULL);
		(&V_15)->___Albedo = L_114;
		G_B22_0 = G_B21_0;
	}

IL_03e6:
	{
		float L_115 = V_13;
		if ((!(((float)L_115) > ((float)(0.0f)))))
		{
			G_B24_0 = G_B22_0;
			goto IL_0414;
		}
		G_B23_0 = G_B22_0;
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_116 = (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)(&(&V_15)->___Albedo);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_117 = L_116;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_118 = (*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_117);
		float L_119 = V_13;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_120;
		L_120 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_118, ((float)il2cpp_codegen_subtract((1.0f), ((float)il2cpp_codegen_multiply((0.449999988f), L_119)))), NULL);
		*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_117 = L_120;
		G_B24_0 = G_B23_0;
	}

IL_0414:
	{
		if (!G_B24_0)
		{
			goto IL_0431;
		}
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_121;
		memset((&L_121), 0, sizeof(L_121));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_121), (0.419999987f), (0.439999998f), (0.479999989f), NULL);
		(&V_15)->___Albedo = L_121;
	}

IL_0431:
	{
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_122 = V_15;
		return L_122;
	}

IL_0434:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_123 = V_0;
		float L_124 = L_123.___Y;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		float L_125;
		L_125 = PayloadRender_Line_m585D11C93BA5AD4FEEC6323F2E1D6B9A5980B59F(L_124, (0.109999999f), (0.00400000019f), NULL);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_126;
		L_126 = PayloadRender_M_m9400B16719658CD34FD3CFA82FA9E8C75FD60D94((0.879999995f), (0.870000005f), (0.839999974f), (1.0f), (0.899999976f), (0.899999976f), (0.899999976f), (0.280000001f), (18.0f), (0.119999997f), NULL);
		V_16 = L_126;
		if ((!(((float)L_125) > ((float)(0.0f)))))
		{
			goto IL_04a5;
		}
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_127 = (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)(&(&V_16)->___Albedo);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_128 = L_127;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_129 = (*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_128);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_130;
		L_130 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_129, (0.860000014f), NULL);
		*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_128 = L_130;
	}

IL_04a5:
	{
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_131 = V_16;
		return L_131;
	}

IL_04a8:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_132;
		L_132 = PayloadRender_M_m9400B16719658CD34FD3CFA82FA9E8C75FD60D94((0.519999981f), (0.550000012f), (0.600000024f), (0.550000012f), (0.850000024f), (0.879999995f), (0.920000017f), (0.899999976f), (46.0f), (0.550000012f), NULL);
		V_17 = L_132;
		(&V_17)->___Metal = (0.699999988f);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_133 = V_17;
		return L_133;
	}

IL_04f0:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_134;
		L_134 = PayloadRender_M_m9400B16719658CD34FD3CFA82FA9E8C75FD60D94((0.959999979f), (0.430000007f), (0.119999997f), (1.0f), (1.0f), (0.800000012f), (0.600000024f), (0.349999994f), (26.0f), (0.100000001f), NULL);
		return L_134;
	}

IL_0528:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_135;
		L_135 = PayloadRender_M_m9400B16719658CD34FD3CFA82FA9E8C75FD60D94((0.0700000003f), (0.0700000003f), (0.0900000036f), (1.0f), (0.800000012f), (0.800000012f), (0.850000024f), (0.449999988f), (40.0f), (0.25f), NULL);
		return L_135;
	}

IL_0560:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_136;
		L_136 = PayloadRender_M_m9400B16719658CD34FD3CFA82FA9E8C75FD60D94((0.0199999996f), (0.0500000007f), (0.119999997f), (1.0f), (0.600000024f), (0.75f), (1.0f), (1.20000005f), (90.0f), (0.699999988f), NULL);
		return L_136;
	}

IL_0598:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_137 = V_0;
		float L_138 = L_137.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_139 = V_0;
		float L_140 = L_139.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_141 = V_0;
		float L_142 = L_141.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_143 = V_0;
		float L_144 = L_143.___Y;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_145;
		L_145 = sqrt(((double)((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(L_138, L_140)), ((float)il2cpp_codegen_multiply(L_142, L_144))))));
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_146;
		L_146 = PayloadRender_M_m9400B16719658CD34FD3CFA82FA9E8C75FD60D94((0.899999976f), (0.889999986f), (0.850000024f), (1.0f), (0.899999976f), (0.899999976f), (0.899999976f), (0.300000012f), (20.0f), (0.119999997f), NULL);
		V_18 = L_146;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_147 = ___0_p;
		NullCheck(L_147);
		float L_148 = L_147->___A;
		float L_149;
		L_149 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA((0.0399999991f), ((float)il2cpp_codegen_multiply(L_148, (0.239999995f))), NULL);
		float L_150;
		L_150 = PayloadRender_Line_m585D11C93BA5AD4FEEC6323F2E1D6B9A5980B59F(((float)L_145), L_149, (0.00499999989f), NULL);
		if ((!(((float)L_150) > ((float)(0.0f)))))
		{
			goto IL_0636;
		}
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_151 = (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)(&(&V_18)->___Albedo);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_152 = L_151;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_153 = (*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_152);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_154;
		L_154 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_153, (0.839999974f), NULL);
		*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_152 = L_154;
	}

IL_0636:
	{
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_155 = V_18;
		return L_155;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 PayloadRender_Cells_mE701F463E8E2334ECAA9C3A611CF03FA2C88042A (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* ___0_p, Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 ___1_h, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___2_lp, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	float V_1 = 0.0f;
	float V_2 = 0.0f;
	float V_3 = 0.0f;
	Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 V_4;
	memset((&V_4), 0, sizeof(V_4));
	float V_5 = 0.0f;
	float V_6 = 0.0f;
	float V_7 = 0.0f;
	{
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_0 = ___0_p;
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 L_1 = ___1_h;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2 = ___2_lp;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		PayloadRender_FaceUv_m80043B41EF1004805E20B0FB665CF214947D3310(L_0, L_1, L_2, (&V_0), (&V_1), (&V_2), (&V_3), NULL);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_3;
		L_3 = PayloadRender_M_m9400B16719658CD34FD3CFA82FA9E8C75FD60D94((0.0399999991f), (0.100000001f), (0.319999993f), (1.0f), (0.620000005f), (0.720000029f), (1.0f), (0.949999988f), (70.0f), (0.419999987f), NULL);
		V_4 = L_3;
		float L_4 = V_2;
		float L_5 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_6;
		L_6 = fabsf(L_5);
		if ((((float)((float)il2cpp_codegen_subtract(L_4, L_6))) < ((float)(0.0219999999f))))
		{
			goto IL_0067;
		}
	}
	{
		float L_7 = V_3;
		float L_8 = V_1;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_9;
		L_9 = fabsf(L_8);
		if ((!(((float)((float)il2cpp_codegen_subtract(L_7, L_9))) < ((float)(0.0219999999f)))))
		{
			goto IL_009f;
		}
	}

IL_0067:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_10;
		L_10 = PayloadRender_M_m9400B16719658CD34FD3CFA82FA9E8C75FD60D94((0.620000005f), (0.639999986f), (0.680000007f), (0.699999988f), (0.899999976f), (0.899999976f), (0.899999976f), (0.600000024f), (36.0f), (0.349999994f), NULL);
		return L_10;
	}

IL_009f:
	{
		float L_11 = V_0;
		float L_12 = V_2;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		float L_13;
		L_13 = PayloadRender_Frac_mCB86369BB619EE9BE740A60F04823BB2377D9BBF(((float)(((float)il2cpp_codegen_add(L_11, L_12))/(0.0719999969f))), NULL);
		V_5 = L_13;
		float L_14 = V_1;
		float L_15 = V_3;
		float L_16;
		L_16 = PayloadRender_Frac_mCB86369BB619EE9BE740A60F04823BB2377D9BBF(((float)(((float)il2cpp_codegen_add(L_14, L_15))/(0.0719999969f))), NULL);
		V_6 = L_16;
		float L_17 = V_5;
		if ((((float)L_17) < ((float)(0.0799999982f))))
		{
			goto IL_00d1;
		}
	}
	{
		float L_18 = V_6;
		if ((!(((float)L_18) < ((float)(0.0799999982f)))))
		{
			goto IL_0109;
		}
	}

IL_00d1:
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_19;
		L_19 = PayloadRender_M_m9400B16719658CD34FD3CFA82FA9E8C75FD60D94((0.720000029f), (0.74000001f), (0.779999971f), (0.899999976f), (0.899999976f), (0.899999976f), (0.949999988f), (0.5f), (40.0f), (0.300000012f), NULL);
		return L_19;
	}

IL_0109:
	{
		float L_20 = V_0;
		float L_21 = V_2;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_22;
		L_22 = floor(((double)((float)(((float)il2cpp_codegen_add(L_20, L_21))/(0.0719999969f)))));
		float L_23 = V_1;
		float L_24 = V_3;
		double L_25;
		L_25 = floor(((double)((float)(((float)il2cpp_codegen_add(L_23, L_24))/(0.0719999969f)))));
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_26 = ___0_p;
		NullCheck(L_26);
		float L_27 = L_26->___Seed;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		float L_28;
		L_28 = PayloadRender_Hash_mAFB67513BCEE7055828B4324DEA16502DC155896(il2cpp_codegen_cast_double_to_int<int32_t>(L_22), il2cpp_codegen_cast_double_to_int<int32_t>(L_25), L_27, NULL);
		V_7 = ((float)il2cpp_codegen_add((0.850000024f), ((float)il2cpp_codegen_multiply((0.300000012f), L_28))));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_29 = (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)(&(&V_4)->___Albedo);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_30 = L_29;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_31 = (*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_30);
		float L_32 = V_7;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_33;
		L_33 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_31, L_32, NULL);
		*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_30 = L_33;
		float L_34 = V_5;
		float L_35;
		L_35 = PayloadRender_Frac_mCB86369BB619EE9BE740A60F04823BB2377D9BBF(((float)il2cpp_codegen_multiply(L_34, (5.0f))), NULL);
		if ((!(((float)L_35) < ((float)(0.0700000003f)))))
		{
			goto IL_019b;
		}
	}
	{
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_36 = V_4;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_37 = L_36.___Albedo;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_38;
		memset((&L_38), 0, sizeof(L_38));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_38), (0.340000004f), (0.419999987f), (0.620000005f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_39;
		L_39 = V3_Lerp_mB6067E0973AEFE35323954D146ED580F05E2D125(L_37, L_38, (0.600000024f), NULL);
		(&V_4)->___Albedo = L_39;
	}

IL_019b:
	{
		Mat_tF560921B2617E9F9BAF232386519B4016C462CA1 L_40 = V_4;
		return L_40;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_FaceUv_m80043B41EF1004805E20B0FB665CF214947D3310 (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* ___0_p, Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 ___1_h, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___2_lp, float* ___3_u, float* ___4_v, float* ___5_hu, float* ___6_hv, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	float V_1 = 0.0f;
	float V_2 = 0.0f;
	float V_3 = 0.0f;
	float V_4 = 0.0f;
	float* G_B9_0 = NULL;
	float* G_B8_0 = NULL;
	float G_B10_0 = 0.0f;
	float* G_B10_1 = NULL;
	{
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_0 = ___0_p;
		NullCheck(L_0);
		int32_t L_1 = L_0->___Kind;
		if (L_1)
		{
			goto IL_00af;
		}
	}
	{
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642 L_2 = ___1_h;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_3 = L_2.___LocalN;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_4 = L_3;
		float L_5 = L_4.___X;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_6;
		L_6 = fabsf(L_5);
		V_2 = L_6;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_7 = L_4;
		float L_8 = L_7.___Y;
		float L_9;
		L_9 = fabsf(L_8);
		V_3 = L_9;
		float L_10 = L_7.___Z;
		float L_11;
		L_11 = fabsf(L_10);
		V_4 = L_11;
		float L_12 = V_2;
		float L_13 = V_3;
		if ((!(((float)L_12) >= ((float)L_13))))
		{
			goto IL_0062;
		}
	}
	{
		float L_14 = V_2;
		float L_15 = V_4;
		if ((!(((float)L_14) >= ((float)L_15))))
		{
			goto IL_0062;
		}
	}
	{
		float* L_16 = ___3_u;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_17 = ___2_lp;
		float L_18 = L_17.___Z;
		*((float*)L_16) = (float)L_18;
		float* L_19 = ___4_v;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_20 = ___2_lp;
		float L_21 = L_20.___Y;
		*((float*)L_19) = (float)L_21;
		float* L_22 = ___5_hu;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_23 = ___0_p;
		NullCheck(L_23);
		float L_24 = L_23->___C;
		*((float*)L_22) = (float)L_24;
		float* L_25 = ___6_hv;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_26 = ___0_p;
		NullCheck(L_26);
		float L_27 = L_26->___B;
		*((float*)L_25) = (float)L_27;
		return;
	}

IL_0062:
	{
		float L_28 = V_3;
		float L_29 = V_4;
		if ((!(((float)L_28) >= ((float)L_29))))
		{
			goto IL_008b;
		}
	}
	{
		float* L_30 = ___3_u;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_31 = ___2_lp;
		float L_32 = L_31.___X;
		*((float*)L_30) = (float)L_32;
		float* L_33 = ___4_v;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_34 = ___2_lp;
		float L_35 = L_34.___Z;
		*((float*)L_33) = (float)L_35;
		float* L_36 = ___5_hu;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_37 = ___0_p;
		NullCheck(L_37);
		float L_38 = L_37->___A;
		*((float*)L_36) = (float)L_38;
		float* L_39 = ___6_hv;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_40 = ___0_p;
		NullCheck(L_40);
		float L_41 = L_40->___C;
		*((float*)L_39) = (float)L_41;
		return;
	}

IL_008b:
	{
		float* L_42 = ___3_u;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_43 = ___2_lp;
		float L_44 = L_43.___X;
		*((float*)L_42) = (float)L_44;
		float* L_45 = ___4_v;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_46 = ___2_lp;
		float L_47 = L_46.___Y;
		*((float*)L_45) = (float)L_47;
		float* L_48 = ___5_hu;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_49 = ___0_p;
		NullCheck(L_49);
		float L_50 = L_49->___A;
		*((float*)L_48) = (float)L_50;
		float* L_51 = ___6_hv;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_52 = ___0_p;
		NullCheck(L_52);
		float L_53 = L_52->___B;
		*((float*)L_51) = (float)L_53;
		return;
	}

IL_00af:
	{
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_54 = ___0_p;
		NullCheck(L_54);
		float L_55 = L_54->___A;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_56 = ___0_p;
		NullCheck(L_56);
		float L_57 = L_56->___B;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_58;
		L_58 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA(L_55, L_57, NULL);
		V_0 = L_58;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_59 = ___2_lp;
		float L_60 = L_59.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_61 = ___2_lp;
		float L_62 = L_61.___X;
		double L_63;
		L_63 = atan2(((double)L_60), ((double)L_62));
		V_1 = ((float)L_63);
		float* L_64 = ___3_u;
		float L_65 = V_1;
		float L_66 = V_0;
		*((float*)L_64) = (float)((float)il2cpp_codegen_multiply(L_65, L_66));
		float* L_67 = ___4_v;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_68 = ___2_lp;
		float L_69 = L_68.___Z;
		*((float*)L_67) = (float)L_69;
		float* L_70 = ___5_hu;
		*((float*)L_70) = (float)(1000.0f);
		float* L_71 = ___6_hv;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_72 = ___0_p;
		NullCheck(L_72);
		float L_73 = L_72->___C;
		if ((((float)L_73) > ((float)(0.0f))))
		{
			G_B9_0 = L_71;
			goto IL_0102;
		}
		G_B8_0 = L_71;
	}
	{
		G_B10_0 = (1000.0f);
		G_B10_1 = G_B8_0;
		goto IL_0108;
	}

IL_0102:
	{
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_74 = ___0_p;
		NullCheck(L_74);
		float L_75 = L_74->___C;
		G_B10_0 = L_75;
		G_B10_1 = G_B9_0;
	}

IL_0108:
	{
		*((float*)G_B10_1) = (float)G_B10_0;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadRender_Stitch_m9496EAB56707B260EE63E06BE961539E8C301B97 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_lp, float ___1_pitch, float ___2_w, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = ___0_lp;
		float L_1 = L_0.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2 = ___0_lp;
		float L_3 = L_2.___Z;
		float L_4 = ___1_pitch;
		float L_5 = ___2_w;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		float L_6;
		L_6 = PayloadRender_Line_m585D11C93BA5AD4FEEC6323F2E1D6B9A5980B59F(((float)il2cpp_codegen_add(L_1, ((float)il2cpp_codegen_multiply(L_3, (0.5f))))), L_4, L_5, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_7 = ___0_lp;
		float L_8 = L_7.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_9 = ___0_lp;
		float L_10 = L_9.___Z;
		float L_11 = ___1_pitch;
		float L_12 = ___2_w;
		float L_13;
		L_13 = PayloadRender_Line_m585D11C93BA5AD4FEEC6323F2E1D6B9A5980B59F(((float)il2cpp_codegen_add(L_8, ((float)il2cpp_codegen_multiply(L_10, (0.300000012f))))), ((float)il2cpp_codegen_multiply(L_11, (1.29999995f))), L_12, NULL);
		V_0 = L_13;
		float L_14 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_15;
		L_15 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA(L_6, L_14, NULL);
		return L_15;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadRender_Line_m585D11C93BA5AD4FEEC6323F2E1D6B9A5980B59F (float ___0_x, float ___1_pitch, float ___2_halfW, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		float L_0 = ___0_x;
		float L_1 = ___1_pitch;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		float L_2;
		L_2 = PayloadRender_Frac_mCB86369BB619EE9BE740A60F04823BB2377D9BBF(((float)il2cpp_codegen_add(((float)(L_0/L_1)), (0.5f))), NULL);
		float L_3 = ___1_pitch;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_4;
		L_4 = fabsf(((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_subtract(L_2, (0.5f))), L_3)));
		float L_5 = ___2_halfW;
		if ((((float)L_4) < ((float)L_5)))
		{
			goto IL_0024;
		}
	}
	{
		return (0.0f);
	}

IL_0024:
	{
		return (1.0f);
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadRender_Hash_mC127B3E3881473AB770AFC6672497E9A6266C5A9 (int32_t ___0_x, int32_t ___1_y, int32_t ___2_z, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = ___0_x;
		int32_t L_1 = ___1_y;
		int32_t L_2 = ___2_z;
		int32_t L_3 = ((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(L_0, ((int32_t)374761393))), ((int32_t)il2cpp_codegen_multiply(L_1, ((int32_t)668265263))))), ((int32_t)il2cpp_codegen_multiply(L_2, ((int32_t)2147483647LL)))));
		int32_t L_4 = ((int32_t)il2cpp_codegen_multiply(((int32_t)(L_3^((int32_t)((uint32_t)L_3>>((int32_t)13))))), ((int32_t)1274126177)));
		return ((float)(((float)((double)(uint32_t)((int32_t)(((int32_t)(L_4^((int32_t)((uint32_t)L_4>>((int32_t)16)))))&((int32_t)16777215)))))/(16777215.0f)));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadRender_Hash_mAFB67513BCEE7055828B4324DEA16502DC155896 (int32_t ___0_x, int32_t ___1_y, float ___2_seed, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		int32_t L_0 = ___0_x;
		int32_t L_1 = ___1_y;
		float L_2 = ___2_seed;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		float L_3;
		L_3 = PayloadRender_Hash_mC127B3E3881473AB770AFC6672497E9A6266C5A9(L_0, L_1, il2cpp_codegen_cast_double_to_int<int32_t>(((float)il2cpp_codegen_multiply(L_2, (977.0f)))), NULL);
		return L_3;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadRender_Noise_mA392E35C9ECF60BDD72D5CCB2921218AA8E0A970 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_p, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	int32_t V_1 = 0;
	int32_t V_2 = 0;
	float V_3 = 0.0f;
	float V_4 = 0.0f;
	float V_5 = 0.0f;
	float V_6 = 0.0f;
	float V_7 = 0.0f;
	float V_8 = 0.0f;
	float V_9 = 0.0f;
	float V_10 = 0.0f;
	float V_11 = 0.0f;
	float V_12 = 0.0f;
	float V_13 = 0.0f;
	float V_14 = 0.0f;
	float V_15 = 0.0f;
	float V_16 = 0.0f;
	float V_17 = 0.0f;
	float V_18 = 0.0f;
	float V_19 = 0.0f;
	float V_20 = 0.0f;
	float V_21 = 0.0f;
	float V_22 = 0.0f;
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = ___0_p;
		float L_1 = L_0.___X;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_2;
		L_2 = floor(((double)L_1));
		V_0 = il2cpp_codegen_cast_double_to_int<int32_t>(L_2);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_3 = ___0_p;
		float L_4 = L_3.___Y;
		double L_5;
		L_5 = floor(((double)L_4));
		V_1 = il2cpp_codegen_cast_double_to_int<int32_t>(L_5);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6 = ___0_p;
		float L_7 = L_6.___Z;
		double L_8;
		L_8 = floor(((double)L_7));
		V_2 = il2cpp_codegen_cast_double_to_int<int32_t>(L_8);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_9 = ___0_p;
		float L_10 = L_9.___X;
		int32_t L_11 = V_0;
		V_3 = ((float)il2cpp_codegen_subtract(L_10, ((float)L_11)));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12 = ___0_p;
		float L_13 = L_12.___Y;
		int32_t L_14 = V_1;
		V_4 = ((float)il2cpp_codegen_subtract(L_13, ((float)L_14)));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_15 = ___0_p;
		float L_16 = L_15.___Z;
		int32_t L_17 = V_2;
		V_5 = ((float)il2cpp_codegen_subtract(L_16, ((float)L_17)));
		float L_18 = V_3;
		float L_19 = V_3;
		float L_20 = V_3;
		V_6 = ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_multiply(L_18, L_19)), ((float)il2cpp_codegen_subtract((3.0f), ((float)il2cpp_codegen_multiply((2.0f), L_20))))));
		float L_21 = V_4;
		float L_22 = V_4;
		float L_23 = V_4;
		V_7 = ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_multiply(L_21, L_22)), ((float)il2cpp_codegen_subtract((3.0f), ((float)il2cpp_codegen_multiply((2.0f), L_23))))));
		float L_24 = V_5;
		float L_25 = V_5;
		float L_26 = V_5;
		V_8 = ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_multiply(L_24, L_25)), ((float)il2cpp_codegen_subtract((3.0f), ((float)il2cpp_codegen_multiply((2.0f), L_26))))));
		int32_t L_27 = V_0;
		int32_t L_28 = V_1;
		int32_t L_29 = V_2;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		float L_30;
		L_30 = PayloadRender_Hash_mC127B3E3881473AB770AFC6672497E9A6266C5A9(L_27, L_28, L_29, NULL);
		V_9 = L_30;
		int32_t L_31 = V_0;
		int32_t L_32 = V_1;
		int32_t L_33 = V_2;
		float L_34;
		L_34 = PayloadRender_Hash_mC127B3E3881473AB770AFC6672497E9A6266C5A9(((int32_t)il2cpp_codegen_add(L_31, 1)), L_32, L_33, NULL);
		V_10 = L_34;
		int32_t L_35 = V_0;
		int32_t L_36 = V_1;
		int32_t L_37 = V_2;
		float L_38;
		L_38 = PayloadRender_Hash_mC127B3E3881473AB770AFC6672497E9A6266C5A9(L_35, ((int32_t)il2cpp_codegen_add(L_36, 1)), L_37, NULL);
		V_11 = L_38;
		int32_t L_39 = V_0;
		int32_t L_40 = V_1;
		int32_t L_41 = V_2;
		float L_42;
		L_42 = PayloadRender_Hash_mC127B3E3881473AB770AFC6672497E9A6266C5A9(((int32_t)il2cpp_codegen_add(L_39, 1)), ((int32_t)il2cpp_codegen_add(L_40, 1)), L_41, NULL);
		V_12 = L_42;
		int32_t L_43 = V_0;
		int32_t L_44 = V_1;
		int32_t L_45 = V_2;
		float L_46;
		L_46 = PayloadRender_Hash_mC127B3E3881473AB770AFC6672497E9A6266C5A9(L_43, L_44, ((int32_t)il2cpp_codegen_add(L_45, 1)), NULL);
		V_13 = L_46;
		int32_t L_47 = V_0;
		int32_t L_48 = V_1;
		int32_t L_49 = V_2;
		float L_50;
		L_50 = PayloadRender_Hash_mC127B3E3881473AB770AFC6672497E9A6266C5A9(((int32_t)il2cpp_codegen_add(L_47, 1)), L_48, ((int32_t)il2cpp_codegen_add(L_49, 1)), NULL);
		V_14 = L_50;
		int32_t L_51 = V_0;
		int32_t L_52 = V_1;
		int32_t L_53 = V_2;
		float L_54;
		L_54 = PayloadRender_Hash_mC127B3E3881473AB770AFC6672497E9A6266C5A9(L_51, ((int32_t)il2cpp_codegen_add(L_52, 1)), ((int32_t)il2cpp_codegen_add(L_53, 1)), NULL);
		V_15 = L_54;
		int32_t L_55 = V_0;
		int32_t L_56 = V_1;
		int32_t L_57 = V_2;
		float L_58;
		L_58 = PayloadRender_Hash_mC127B3E3881473AB770AFC6672497E9A6266C5A9(((int32_t)il2cpp_codegen_add(L_55, 1)), ((int32_t)il2cpp_codegen_add(L_56, 1)), ((int32_t)il2cpp_codegen_add(L_57, 1)), NULL);
		V_16 = L_58;
		float L_59 = V_9;
		float L_60 = V_10;
		float L_61 = V_9;
		float L_62 = V_6;
		V_17 = ((float)il2cpp_codegen_add(L_59, ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_subtract(L_60, L_61)), L_62))));
		float L_63 = V_11;
		float L_64 = V_12;
		float L_65 = V_11;
		float L_66 = V_6;
		V_18 = ((float)il2cpp_codegen_add(L_63, ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_subtract(L_64, L_65)), L_66))));
		float L_67 = V_13;
		float L_68 = V_14;
		float L_69 = V_13;
		float L_70 = V_6;
		V_19 = ((float)il2cpp_codegen_add(L_67, ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_subtract(L_68, L_69)), L_70))));
		float L_71 = V_15;
		float L_72 = V_16;
		float L_73 = V_15;
		float L_74 = V_6;
		V_20 = ((float)il2cpp_codegen_add(L_71, ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_subtract(L_72, L_73)), L_74))));
		float L_75 = V_17;
		float L_76 = V_18;
		float L_77 = V_17;
		float L_78 = V_7;
		V_21 = ((float)il2cpp_codegen_add(L_75, ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_subtract(L_76, L_77)), L_78))));
		float L_79 = V_19;
		float L_80 = V_20;
		float L_81 = V_19;
		float L_82 = V_7;
		V_22 = ((float)il2cpp_codegen_add(L_79, ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_subtract(L_80, L_81)), L_82))));
		float L_83 = V_21;
		float L_84 = V_22;
		float L_85 = V_21;
		float L_86 = V_8;
		return ((float)il2cpp_codegen_add(L_83, ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_subtract(L_84, L_85)), L_86))));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool PayloadRender_Trace_m537188514DC0B279D080B22F51DB8A5234E6FD59 (List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* ___0_scene, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_o, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___2_d, float ___3_maxT, int32_t ___4_skip, Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642* ___5_best, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_get_Count_mA4B6A7483D74113D7978E2DA5D1DEE4F906ECC23_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_get_Item_m658740103DB2DE2E2FF16DEFD463120C34BBE346_RuntimeMethod_var);
		s_Il2CppMethodInitialized = true;
	}
	Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* V_0 = NULL;
	int32_t V_1 = 0;
	Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* V_2 = NULL;
	float V_3 = 0.0f;
	float V_4 = 0.0f;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_5;
	memset((&V_5), 0, sizeof(V_5));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_6;
	memset((&V_6), 0, sizeof(V_6));
	float V_7 = 0.0f;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_8;
	memset((&V_8), 0, sizeof(V_8));
	{
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642* L_0 = ___5_best;
		il2cpp_codegen_initobj(L_0, sizeof(Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642));
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642* L_1 = ___5_best;
		float L_2 = ___3_maxT;
		L_1->___T = L_2;
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642* L_3 = ___5_best;
		L_3->___Index = (-1);
		V_1 = 0;
		goto IL_00e0;
	}

IL_001f:
	{
		int32_t L_4 = V_1;
		int32_t L_5 = ___4_skip;
		if ((((int32_t)L_4) == ((int32_t)L_5)))
		{
			goto IL_00dc;
		}
	}
	{
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_6 = ___0_scene;
		int32_t L_7 = V_1;
		NullCheck(L_6);
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_8;
		L_8 = List_1_get_Item_m658740103DB2DE2E2FF16DEFD463120C34BBE346(L_6, L_7, List_1_get_Item_m658740103DB2DE2E2FF16DEFD463120C34BBE346_RuntimeMethod_var);
		V_2 = L_8;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_9 = ___1_o;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_10 = V_2;
		NullCheck(L_10);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_11 = L_10->___Center;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12;
		L_12 = V3_op_Subtraction_mBF754B8B036EFB8C8B32E2C6A9799AFC1B245482(L_9, L_11, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_13 = L_12;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_14 = ___2_d;
		float L_15;
		L_15 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_13, L_14, NULL);
		V_3 = L_15;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_16 = L_13;
		float L_17;
		L_17 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_16, L_16, NULL);
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_18 = V_2;
		NullCheck(L_18);
		float L_19 = L_18->___Radius;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_20 = V_2;
		NullCheck(L_20);
		float L_21 = L_20->___Radius;
		V_4 = ((float)il2cpp_codegen_subtract(L_17, ((float)il2cpp_codegen_multiply(L_19, L_21))));
		float L_22 = V_4;
		if ((!(((float)L_22) > ((float)(0.0f)))))
		{
			goto IL_006a;
		}
	}
	{
		float L_23 = V_3;
		if ((((float)L_23) > ((float)(0.0f))))
		{
			goto IL_00dc;
		}
	}

IL_006a:
	{
		float L_24 = V_3;
		float L_25 = V_3;
		float L_26 = V_4;
		if ((((float)((float)il2cpp_codegen_subtract(((float)il2cpp_codegen_multiply(L_24, L_25)), L_26))) < ((float)(0.0f))))
		{
			goto IL_00dc;
		}
	}
	{
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_27 = V_2;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_28 = ___1_o;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_29 = V_2;
		NullCheck(L_29);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_30 = L_29->___O;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_31;
		L_31 = V3_op_Subtraction_mBF754B8B036EFB8C8B32E2C6A9799AFC1B245482(L_28, L_30, NULL);
		NullCheck(L_27);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_32;
		L_32 = Prim_ToLocal_m3C6745FB67593049B03ED7B23000B1D2CE8618B2(L_27, L_31, NULL);
		V_5 = L_32;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_33 = V_2;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_34 = ___2_d;
		NullCheck(L_33);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_35;
		L_35 = Prim_ToLocal_m3C6745FB67593049B03ED7B23000B1D2CE8618B2(L_33, L_34, NULL);
		V_6 = L_35;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_36 = V_2;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_37 = V_5;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_38 = V_6;
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642* L_39 = ___5_best;
		float L_40 = L_39->___T;
		NullCheck(L_36);
		bool L_41;
		L_41 = Prim_Intersect_m6BE49C58D6429F03E78403320F517788BD7D3D84(L_36, L_37, L_38, L_40, (&V_7), (&V_8), NULL);
		if (!L_41)
		{
			goto IL_00dc;
		}
	}
	{
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642* L_42 = ___5_best;
		float L_43 = V_7;
		L_42->___T = L_43;
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642* L_44 = ___5_best;
		int32_t L_45 = V_1;
		L_44->___Index = L_45;
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642* L_46 = ___5_best;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_47 = V_8;
		L_46->___LocalN = L_47;
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642* L_48 = ___5_best;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_49 = V_5;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_50 = V_6;
		float L_51 = V_7;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_52;
		L_52 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_50, L_51, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_53;
		L_53 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_49, L_52, NULL);
		L_48->___Local = L_53;
	}

IL_00dc:
	{
		int32_t L_54 = V_1;
		V_1 = ((int32_t)il2cpp_codegen_add(L_54, 1));
	}

IL_00e0:
	{
		int32_t L_55 = V_1;
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_56 = ___0_scene;
		NullCheck(L_56);
		int32_t L_57;
		L_57 = List_1_get_Count_mA4B6A7483D74113D7978E2DA5D1DEE4F906ECC23_inline(L_56, List_1_get_Count_mA4B6A7483D74113D7978E2DA5D1DEE4F906ECC23_RuntimeMethod_var);
		if ((((int32_t)L_55) < ((int32_t)L_57)))
		{
			goto IL_001f;
		}
	}
	{
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642* L_58 = ___5_best;
		int32_t L_59 = L_58->___Index;
		if ((((int32_t)L_59) >= ((int32_t)0)))
		{
			goto IL_00f8;
		}
	}
	{
		return (bool)0;
	}

IL_00f8:
	{
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_60 = ___0_scene;
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642* L_61 = ___5_best;
		int32_t L_62 = L_61->___Index;
		NullCheck(L_60);
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_63;
		L_63 = List_1_get_Item_m658740103DB2DE2E2FF16DEFD463120C34BBE346(L_60, L_62, List_1_get_Item_m658740103DB2DE2E2FF16DEFD463120C34BBE346_RuntimeMethod_var);
		V_0 = L_63;
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642* L_64 = ___5_best;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_65 = ___1_o;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_66 = ___2_d;
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642* L_67 = ___5_best;
		float L_68 = L_67->___T;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_69;
		L_69 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_66, L_68, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_70;
		L_70 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_65, L_69, NULL);
		L_64->___P = L_70;
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642* L_71 = ___5_best;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_72 = V_0;
		NullCheck(L_72);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_73 = L_72->___U;
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642* L_74 = ___5_best;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_75 = (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)(&L_74->___LocalN);
		float L_76 = L_75->___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_77;
		L_77 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_73, L_76, NULL);
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_78 = V_0;
		NullCheck(L_78);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_79 = L_78->___V;
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642* L_80 = ___5_best;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_81 = (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)(&L_80->___LocalN);
		float L_82 = L_81->___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_83;
		L_83 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_79, L_82, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_84;
		L_84 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_77, L_83, NULL);
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_85 = V_0;
		NullCheck(L_85);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_86 = L_85->___W;
		Hit_t85954083459D79438FF7AFF6F9BAF1E912D62642* L_87 = ___5_best;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_88 = (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)(&L_87->___LocalN);
		float L_89 = L_88->___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_90;
		L_90 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_86, L_89, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_91;
		L_91 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_84, L_90, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_92;
		L_92 = V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892(L_91, NULL);
		L_71->___N = L_92;
		return (bool)1;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1 (float ___0_x, float ___1_y, float ___2_z, const RuntimeMethod* method) 
{
	{
		float L_0 = ___0_x;
		float L_1 = ___1_y;
		float L_2 = ___2_z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_3;
		memset((&L_3), 0, sizeof(L_3));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_3), L_0, L_1, L_2, NULL);
		return L_3;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* PayloadRender_Scene_mF202D14509424834B06B24444ADF5AC9BCD5B49A (String_t* ___0_form, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Dictionary_2_TryGetValue_m269D507F7E00AC2566CC52C93E3E3A084A03B5F6_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Dictionary_2_set_Item_mB2347972BD237E936BF2A0F5921FF0335524879C_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral00990E38CA33B93B1431473E05022A9DCC677F93);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral1C43EC867F20B8DB8FA13AC94D6DB825367CF26C);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral3485BA793A227DB725EB436AF2FC1354566DEDF7);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral3C4F52AE03EA8FEF5DEAA4BF1DCF2792C970C821);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral3D6DAD0DC8E1D6D4D37111226BD75DCE876A1C7E);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral4A3A8F16E29B08914BAD480CC20AF8B66E850DAB);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral4BF88E349C859F17645B0DD8922125ABAA3BAA05);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral52A70EE7ABEE0DD947480638E3B909A9A19C1D4E);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral9053C7BE1B5533FB96F3DE70C35EA0FFAB4FE19E);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralA9F5008AA90307FD7521614307E34133396662CA);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralBF8DB1FCA3176EF0EAAE60DA02213D8D74498AFE);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralD3CB15C4F0E233957BA9ED742AFB0EBF860C118F);
		s_Il2CppMethodInitialized = true;
	}
	String_t* V_0 = NULL;
	Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A* V_1 = NULL;
	bool V_2 = false;
	List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* V_3 = NULL;
	Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* V_4 = NULL;
	List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* V_5 = NULL;
	uint32_t V_6 = 0;
	String_t* G_B3_0 = NULL;
	{
		String_t* L_0 = ___0_form;
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = PayloadForms_KnownForm_mF1438848D2B6822053BEC97A6A0E610587144811(L_0, NULL);
		if (L_1)
		{
			goto IL_000f;
		}
	}
	{
		G_B3_0 = _stringLiteral3C4F52AE03EA8FEF5DEAA4BF1DCF2792C970C821;
		goto IL_0010;
	}

IL_000f:
	{
		String_t* L_2 = ___0_form;
		G_B3_0 = L_2;
	}

IL_0010:
	{
		V_0 = G_B3_0;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A* L_3 = ((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___Scenes;
		V_1 = L_3;
		V_2 = (bool)0;
	}
	{
		auto __finallyBlock = il2cpp::utils::Finally([&]
		{

FINALLY_024a:
			{
				{
					bool L_4 = V_2;
					if (!L_4)
					{
						goto IL_0253;
					}
				}
				{
					Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A* L_5 = V_1;
					Monitor_Exit_m05B2CF037E2214B3208198C282490A2A475653FA(L_5, NULL);
				}

IL_0253:
				{
					return;
				}
			}
		});
		try
		{
			{
				Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A* L_6 = V_1;
				Monitor_Enter_m3CDB589DA1300B513D55FDCFB52B63E879794149(L_6, (&V_2), NULL);
				il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
				Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A* L_7 = ((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___Scenes;
				String_t* L_8 = V_0;
				NullCheck(L_7);
				bool L_9;
				L_9 = Dictionary_2_TryGetValue_m269D507F7E00AC2566CC52C93E3E3A084A03B5F6(L_7, L_8, (&V_3), Dictionary_2_TryGetValue_m269D507F7E00AC2566CC52C93E3E3A084A03B5F6_RuntimeMethod_var);
				if (!L_9)
				{
					goto IL_0038_1;
				}
			}
			{
				List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_10 = V_3;
				V_5 = L_10;
				goto IL_0254;
			}

IL_0038_1:
			{
				Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_11 = (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD*)il2cpp_codegen_object_new(Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD_il2cpp_TypeInfo_var);
				Builder__ctor_m0F97E24000AE731D157CC3CE7CC858AE13978122(L_11, NULL);
				V_4 = L_11;
				String_t* L_12 = V_0;
				uint32_t L_13;
				L_13 = U3CPrivateImplementationDetailsU3E_ComputeStringHash_mC32B6AAD7FBB5563CF84923C7FCAA71C251E5DF0(L_12, NULL);
				V_6 = L_13;
				uint32_t L_14 = V_6;
				if ((!(((uint32_t)L_14) <= ((uint32_t)((int32_t)1607822841)))))
				{
					goto IL_009c_1;
				}
			}
			{
				uint32_t L_15 = V_6;
				if ((!(((uint32_t)L_15) <= ((uint32_t)((int32_t)380928450)))))
				{
					goto IL_0076_1;
				}
			}
			{
				uint32_t L_16 = V_6;
				if ((((int32_t)L_16) == ((int32_t)((int32_t)301197764))))
				{
					goto IL_00f1_1;
				}
			}
			{
				uint32_t L_17 = V_6;
				if ((((int32_t)L_17) == ((int32_t)((int32_t)380928450))))
				{
					goto IL_01a5_1;
				}
			}
			{
				goto IL_0226_1;
			}

IL_0076_1:
			{
				uint32_t L_18 = V_6;
				if ((((int32_t)L_18) == ((int32_t)((int32_t)910182291))))
				{
					goto IL_011b_1;
				}
			}
			{
				uint32_t L_19 = V_6;
				if ((((int32_t)L_19) == ((int32_t)((int32_t)1231151528))))
				{
					goto IL_0193_1;
				}
			}
			{
				uint32_t L_20 = V_6;
				if ((((int32_t)L_20) == ((int32_t)((int32_t)1607822841))))
				{
					goto IL_0106_1;
				}
			}
			{
				goto IL_0226_1;
			}

IL_009c_1:
			{
				uint32_t L_21 = V_6;
				if ((!(((uint32_t)L_21) <= ((uint32_t)((int32_t)-1990644717)))))
				{
					goto IL_00cb_1;
				}
			}
			{
				uint32_t L_22 = V_6;
				if ((((int32_t)L_22) == ((int32_t)((int32_t)2112007601))))
				{
					goto IL_015a_1;
				}
			}
			{
				uint32_t L_23 = V_6;
				if ((((int32_t)L_23) == ((int32_t)((int32_t)-2047630041))))
				{
					goto IL_016f_1;
				}
			}
			{
				uint32_t L_24 = V_6;
				if ((((int32_t)L_24) == ((int32_t)((int32_t)-1990644717))))
				{
					goto IL_0145_1;
				}
			}
			{
				goto IL_0226_1;
			}

IL_00cb_1:
			{
				uint32_t L_25 = V_6;
				if ((((int32_t)L_25) == ((int32_t)((int32_t)-1823902856))))
				{
					goto IL_0181_1;
				}
			}
			{
				uint32_t L_26 = V_6;
				if ((((int32_t)L_26) == ((int32_t)((int32_t)-1523285333))))
				{
					goto IL_01b4_1;
				}
			}
			{
				uint32_t L_27 = V_6;
				if ((((int32_t)L_27) == ((int32_t)((int32_t)-772103730))))
				{
					goto IL_0130_1;
				}
			}
			{
				goto IL_0226_1;
			}

IL_00f1_1:
			{
				String_t* L_28 = V_0;
				bool L_29;
				L_29 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_28, _stringLiteralBF8DB1FCA3176EF0EAAE60DA02213D8D74498AFE, NULL);
				if (L_29)
				{
					goto IL_01c3_1;
				}
			}
			{
				goto IL_0226_1;
			}

IL_0106_1:
			{
				String_t* L_30 = V_0;
				bool L_31;
				L_31 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_30, _stringLiteral9053C7BE1B5533FB96F3DE70C35EA0FFAB4FE19E, NULL);
				if (L_31)
				{
					goto IL_01cc_1;
				}
			}
			{
				goto IL_0226_1;
			}

IL_011b_1:
			{
				String_t* L_32 = V_0;
				bool L_33;
				L_33 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_32, _stringLiteralD3CB15C4F0E233957BA9ED742AFB0EBF860C118F, NULL);
				if (L_33)
				{
					goto IL_01d5_1;
				}
			}
			{
				goto IL_0226_1;
			}

IL_0130_1:
			{
				String_t* L_34 = V_0;
				bool L_35;
				L_35 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_34, _stringLiteral1C43EC867F20B8DB8FA13AC94D6DB825367CF26C, NULL);
				if (L_35)
				{
					goto IL_01de_1;
				}
			}
			{
				goto IL_0226_1;
			}

IL_0145_1:
			{
				String_t* L_36 = V_0;
				bool L_37;
				L_37 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_36, _stringLiteral4BF88E349C859F17645B0DD8922125ABAA3BAA05, NULL);
				if (L_37)
				{
					goto IL_01e7_1;
				}
			}
			{
				goto IL_0226_1;
			}

IL_015a_1:
			{
				String_t* L_38 = V_0;
				bool L_39;
				L_39 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_38, _stringLiteral4A3A8F16E29B08914BAD480CC20AF8B66E850DAB, NULL);
				if (L_39)
				{
					goto IL_01f0_1;
				}
			}
			{
				goto IL_0226_1;
			}

IL_016f_1:
			{
				String_t* L_40 = V_0;
				bool L_41;
				L_41 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_40, _stringLiteral3485BA793A227DB725EB436AF2FC1354566DEDF7, NULL);
				if (L_41)
				{
					goto IL_01f9_1;
				}
			}
			{
				goto IL_0226_1;
			}

IL_0181_1:
			{
				String_t* L_42 = V_0;
				bool L_43;
				L_43 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_42, _stringLiteral00990E38CA33B93B1431473E05022A9DCC677F93, NULL);
				if (L_43)
				{
					goto IL_0202_1;
				}
			}
			{
				goto IL_0226_1;
			}

IL_0193_1:
			{
				String_t* L_44 = V_0;
				bool L_45;
				L_45 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_44, _stringLiteral52A70EE7ABEE0DD947480638E3B909A9A19C1D4E, NULL);
				if (L_45)
				{
					goto IL_020b_1;
				}
			}
			{
				goto IL_0226_1;
			}

IL_01a5_1:
			{
				String_t* L_46 = V_0;
				bool L_47;
				L_47 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_46, _stringLiteralA9F5008AA90307FD7521614307E34133396662CA, NULL);
				if (L_47)
				{
					goto IL_0214_1;
				}
			}
			{
				goto IL_0226_1;
			}

IL_01b4_1:
			{
				String_t* L_48 = V_0;
				bool L_49;
				L_49 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_48, _stringLiteral3D6DAD0DC8E1D6D4D37111226BD75DCE876A1C7E, NULL);
				if (L_49)
				{
					goto IL_021d_1;
				}
			}
			{
				goto IL_0226_1;
			}

IL_01c3_1:
			{
				Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_50 = V_4;
				il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
				PayloadRender_Cubesat_m569CAEAD9D5873EC30CE7562845619F6683DDD56(L_50, NULL);
				goto IL_022d_1;
			}

IL_01cc_1:
			{
				Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_51 = V_4;
				il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
				PayloadRender_BusSat_m4800BB30BDA2347220667C6CE5F3F617966FD7D2(L_51, NULL);
				goto IL_022d_1;
			}

IL_01d5_1:
			{
				Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_52 = V_4;
				il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
				PayloadRender_Weather_mC8ADB2608AE61D9261571176769AD0165EDA58E4(L_52, NULL);
				goto IL_022d_1;
			}

IL_01de_1:
			{
				Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_53 = V_4;
				il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
				PayloadRender_Comms_m2CE72A55FEB0AF6B819EAB02732355BA6DD6862F(L_53, NULL);
				goto IL_022d_1;
			}

IL_01e7_1:
			{
				Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_54 = V_4;
				il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
				PayloadRender_Telescope_m9E7DE068E58F019E17C7B5EE9B77EEBF98C130DD(L_54, NULL);
				goto IL_022d_1;
			}

IL_01f0_1:
			{
				Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_55 = V_4;
				il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
				PayloadRender_Probe_m6FB885F57F94D43D6769B849495A52BBB1F34B17(L_55, NULL);
				goto IL_022d_1;
			}

IL_01f9_1:
			{
				Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_56 = V_4;
				il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
				PayloadRender_Cargo_m02A2F4319102F01B39154A358AC3E2A3DFD476C2(L_56, NULL);
				goto IL_022d_1;
			}

IL_0202_1:
			{
				Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_57 = V_4;
				il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
				PayloadRender_Crew_m88C2F633AE6D651D7E02A5F3BA86B2491372A84D(L_57, NULL);
				goto IL_022d_1;
			}

IL_020b_1:
			{
				Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_58 = V_4;
				il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
				PayloadRender_Dispenser_mB71E6312D962D8722CCC8D2AF8D6DAE85176648F(L_58, NULL);
				goto IL_022d_1;
			}

IL_0214_1:
			{
				Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_59 = V_4;
				il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
				PayloadRender_Relay_mB44BDFC48EEAE2E7A17D5E6156A4997516EC1304(L_59, NULL);
				goto IL_022d_1;
			}

IL_021d_1:
			{
				Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_60 = V_4;
				il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
				PayloadRender_Lander_m0FCDC40AF68DF2D14FE60B4452A077ED07BB9827(L_60, NULL);
				goto IL_022d_1;
			}

IL_0226_1:
			{
				Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_61 = V_4;
				il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
				PayloadRender_MassBox_m258187CB848705238D8FB067EC6EDB6B33979073(L_61, NULL);
			}

IL_022d_1:
			{
				il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
				Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A* L_62 = ((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___Scenes;
				String_t* L_63 = V_0;
				Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_64 = V_4;
				NullCheck(L_64);
				List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_65 = L_64->___Parts;
				NullCheck(L_62);
				Dictionary_2_set_Item_mB2347972BD237E936BF2A0F5921FF0335524879C(L_62, L_63, L_65, Dictionary_2_set_Item_mB2347972BD237E936BF2A0F5921FF0335524879C_RuntimeMethod_var);
				Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_66 = V_4;
				NullCheck(L_66);
				List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_67 = L_66->___Parts;
				V_5 = L_67;
				goto IL_0254;
			}
		}
		catch(Il2CppExceptionWrapper& e)
		{
			__finallyBlock.StoreException(e.ex);
		}
	}

IL_0254:
	{
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_68 = V_5;
		return L_68;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t PayloadRender_PartCount_mDB2BA5BAA6B6A3EF6B1226F1987DB0FF85F15E2F (String_t* ___0_form, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_get_Count_mA4B6A7483D74113D7978E2DA5D1DEE4F906ECC23_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		String_t* L_0 = ___0_form;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_1;
		L_1 = PayloadRender_Scene_mF202D14509424834B06B24444ADF5AC9BCD5B49A(L_0, NULL);
		NullCheck(L_1);
		int32_t L_2;
		L_2 = List_1_get_Count_mA4B6A7483D74113D7978E2DA5D1DEE4F906ECC23_inline(L_1, List_1_get_Count_mA4B6A7483D74113D7978E2DA5D1DEE4F906ECC23_RuntimeMethod_var);
		return L_2;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR HashSet_1_tA7CAED1C5D01DEEEA947992FCF58776FEDC59BAC* PayloadRender_Surfaces_m5E80A0ACC370F5FC1A9209C34B8B051A84455A00 (String_t* ___0_form, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Enumerator_Dispose_mF1558E7B7B84D786841C7B736ECC77BEED4B1367_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Enumerator_MoveNext_mD22EC89FDB16CDABC6AB475A8E71C56858023AC5_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Enumerator_get_Current_m7673828665C413CFC1943B4A6DB0F0148D99596B_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&HashSet_1_Add_m4351A844FAB44D68332AEF82045AE45A6340E64C_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&HashSet_1__ctor_m15C5D34CE1AC392262EDDB77250F68CEF3522CF4_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&HashSet_1_tA7CAED1C5D01DEEEA947992FCF58776FEDC59BAC_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_GetEnumerator_mCD2F70F8886ECEF58C2B0ACD4CC85F99A80733B7_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	HashSet_1_tA7CAED1C5D01DEEEA947992FCF58776FEDC59BAC* V_0 = NULL;
	Enumerator_t785E35998B1974B111A16DAFC5F62FCDEC615835 V_1;
	memset((&V_1), 0, sizeof(V_1));
	Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* V_2 = NULL;
	{
		HashSet_1_tA7CAED1C5D01DEEEA947992FCF58776FEDC59BAC* L_0 = (HashSet_1_tA7CAED1C5D01DEEEA947992FCF58776FEDC59BAC*)il2cpp_codegen_object_new(HashSet_1_tA7CAED1C5D01DEEEA947992FCF58776FEDC59BAC_il2cpp_TypeInfo_var);
		HashSet_1__ctor_m15C5D34CE1AC392262EDDB77250F68CEF3522CF4(L_0, HashSet_1__ctor_m15C5D34CE1AC392262EDDB77250F68CEF3522CF4_RuntimeMethod_var);
		V_0 = L_0;
		String_t* L_1 = ___0_form;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_2;
		L_2 = PayloadRender_Scene_mF202D14509424834B06B24444ADF5AC9BCD5B49A(L_1, NULL);
		NullCheck(L_2);
		Enumerator_t785E35998B1974B111A16DAFC5F62FCDEC615835 L_3;
		L_3 = List_1_GetEnumerator_mCD2F70F8886ECEF58C2B0ACD4CC85F99A80733B7(L_2, List_1_GetEnumerator_mCD2F70F8886ECEF58C2B0ACD4CC85F99A80733B7_RuntimeMethod_var);
		V_1 = L_3;
	}
	{
		auto __finallyBlock = il2cpp::utils::Finally([&]
		{

FINALLY_0034:
			{
				Enumerator_Dispose_mF1558E7B7B84D786841C7B736ECC77BEED4B1367((&V_1), Enumerator_Dispose_mF1558E7B7B84D786841C7B736ECC77BEED4B1367_RuntimeMethod_var);
				return;
			}
		});
		try
		{
			{
				goto IL_0029_1;
			}

IL_0014_1:
			{
				Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_4;
				L_4 = Enumerator_get_Current_m7673828665C413CFC1943B4A6DB0F0148D99596B_inline((&V_1), Enumerator_get_Current_m7673828665C413CFC1943B4A6DB0F0148D99596B_RuntimeMethod_var);
				V_2 = L_4;
				HashSet_1_tA7CAED1C5D01DEEEA947992FCF58776FEDC59BAC* L_5 = V_0;
				Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_6 = V_2;
				NullCheck(L_6);
				int32_t L_7 = L_6->___Surface;
				NullCheck(L_5);
				bool L_8;
				L_8 = HashSet_1_Add_m4351A844FAB44D68332AEF82045AE45A6340E64C(L_5, L_7, HashSet_1_Add_m4351A844FAB44D68332AEF82045AE45A6340E64C_RuntimeMethod_var);
			}

IL_0029_1:
			{
				bool L_9;
				L_9 = Enumerator_MoveNext_mD22EC89FDB16CDABC6AB475A8E71C56858023AC5((&V_1), Enumerator_MoveNext_mD22EC89FDB16CDABC6AB475A8E71C56858023AC5_RuntimeMethod_var);
				if (L_9)
				{
					goto IL_0014_1;
				}
			}
			{
				goto IL_0042;
			}
		}
		catch(Il2CppExceptionWrapper& e)
		{
			__finallyBlock.StoreException(e.ex);
		}
	}

IL_0042:
	{
		HashSet_1_tA7CAED1C5D01DEEEA947992FCF58776FEDC59BAC* L_10 = V_0;
		return L_10;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_MassBox_m258187CB848705238D8FB067EC6EDB6B33979073 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_0 = ___0_b;
		NullCheck(L_0);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_0, (0.0f), (0.0f), (0.0f), (0.899999976f), (0.620000005f), (0.699999988f), 3, (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_1 = ___0_b;
		NullCheck(L_1);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_1, (0.0f), (-0.129999995f), (0.0f), (0.930000007f), (0.379999995f), (0.730000019f), 0, (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_2 = ___0_b;
		NullCheck(L_2);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_2, (0.0f), (0.0700000003f), (0.0f), (0.939999998f), (0.0500000007f), (0.74000001f), 6, (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_3 = ___0_b;
		NullCheck(L_3);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_3, (0.0f), (0.330000013f), (0.0f), (0.5f), (0.0399999991f), (0.400000006f), 5, (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_4 = ___0_b;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_5;
		L_5 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.200000003f), (0.349999994f), (0.100000001f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6;
		L_6 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.200000003f), (0.899999976f), (0.100000001f), NULL);
		NullCheck(L_4);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_4, L_5, L_6, (0.0140000004f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_7 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8;
		L_8 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.200000003f), (0.910000026f), (0.100000001f), NULL);
		NullCheck(L_7);
		Builder_Ball_m2926A9ADD07DBA2D6A030098084DB1CB63778121(L_7, L_8, (0.0299999993f), 6, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_9 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10;
		L_10 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.200000003f), (0.180000007f), (0.360000014f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_11;
		L_11 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.150000006f), (1.0f), NULL);
		NullCheck(L_9);
		Builder_Dish_mCA47CAB74FD0464BEEF7DF5630E487EC9CAD4A91(L_9, L_10, L_11, (0.109999999f), (0.200000003f), ((int32_t)9), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_12 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_13;
		L_13 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.200000003f), (0.180000007f), (0.360000014f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_14;
		L_14 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.200000003f), (0.180000007f), (0.439999998f), NULL);
		NullCheck(L_12);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_12, L_13, L_14, (0.00999999978f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_15 = ___0_b;
		NullCheck(L_15);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_15, (0.460000008f), (-0.200000003f), (0.200000003f), (0.0399999991f), (0.100000001f), (0.100000001f), 7, (0.0f), NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Cubesat_m569CAEAD9D5873EC30CE7562845619F6683DDD56 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	float V_1 = 0.0f;
	float V_2 = 0.0f;
	float G_B4_0 = 0.0f;
	float G_B7_0 = 0.0f;
	{
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_0 = ___0_b;
		NullCheck(L_0);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_0, (0.0f), (0.0f), (0.0f), (0.419999987f), (0.660000026f), (0.419999987f), 2, (0.0f), NULL);
		V_0 = 0;
		goto IL_0078;
	}

IL_002e:
	{
		int32_t L_1 = V_0;
		if (!((int32_t)(L_1&1)))
		{
			goto IL_003a;
		}
	}
	{
		G_B4_0 = (0.215000004f);
		goto IL_003f;
	}

IL_003a:
	{
		G_B4_0 = (-0.215000004f);
	}

IL_003f:
	{
		V_1 = G_B4_0;
		int32_t L_2 = V_0;
		if (!((int32_t)(L_2&2)))
		{
			goto IL_004c;
		}
	}
	{
		G_B7_0 = (0.215000004f);
		goto IL_0051;
	}

IL_004c:
	{
		G_B7_0 = (-0.215000004f);
	}

IL_0051:
	{
		V_2 = G_B7_0;
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_3 = ___0_b;
		float L_4 = V_1;
		float L_5 = V_2;
		NullCheck(L_3);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_3, L_4, (0.0f), L_5, (0.0450000018f), (0.720000029f), (0.0450000018f), 5, (0.0f), NULL);
		int32_t L_6 = V_0;
		V_0 = ((int32_t)il2cpp_codegen_add(L_6, 1));
	}

IL_0078:
	{
		int32_t L_7 = V_0;
		if ((((int32_t)L_7) < ((int32_t)4)))
		{
			goto IL_002e;
		}
	}
	{
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_8 = ___0_b;
		NullCheck(L_8);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_8, (0.0f), (0.344999999f), (0.0f), (0.400000006f), (0.0299999993f), (0.400000006f), 0, (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_9 = ___0_b;
		NullCheck(L_9);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_9, (0.0f), (-0.344999999f), (0.0f), (0.400000006f), (0.0299999993f), (0.400000006f), 0, (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_10 = ___0_b;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_11;
		L_11 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.239999995f), (0.119999997f), (0.0f), NULL);
		NullCheck(L_10);
		Builder_Wing_m4000246F5FF33DAB01DA6B33EB1C083BB206263C(L_10, L_11, (-1.0f), 2, (0.400000006f), (0.5f), (0.0199999996f), (18.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_12 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_13;
		L_13 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.239999995f), (0.119999997f), (0.0f), NULL);
		NullCheck(L_12);
		Builder_Wing_m4000246F5FF33DAB01DA6B33EB1C083BB206263C(L_12, L_13, (1.0f), 2, (0.400000006f), (0.5f), (0.0199999996f), (18.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_14 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_15;
		L_15 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.100000001f), (0.360000014f), (0.100000001f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_16;
		L_16 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.159999996f), (0.949999988f), (0.140000001f), NULL);
		NullCheck(L_14);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_14, L_15, L_16, (0.0109999999f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_17 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_18;
		L_18 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.159999996f), (0.959999979f), (0.140000001f), NULL);
		NullCheck(L_17);
		Builder_Ball_m2926A9ADD07DBA2D6A030098084DB1CB63778121(L_17, L_18, (0.0219999999f), 6, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_19 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_20;
		L_20 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.119999997f), (0.360000014f), (-0.100000001f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_21;
		L_21 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.400000006f), (0.75f), (-0.200000003f), NULL);
		NullCheck(L_19);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_19, L_20, L_21, (0.00800000038f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_22 = ___0_b;
		NullCheck(L_22);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_22, (0.0f), (0.0500000007f), (0.224999994f), (0.119999997f), (0.119999997f), (0.0199999996f), 8, (0.0f), NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_BusSat_m4800BB30BDA2347220667C6CE5F3F617966FD7D2 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_0 = ___0_b;
		NullCheck(L_0);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_0, (0.0f), (0.0f), (0.0f), (0.600000024f), (0.560000002f), (0.600000024f), 0, (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_1 = ___0_b;
		NullCheck(L_1);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_1, (0.0f), (0.300000012f), (0.0f), (0.620000005f), (0.0500000007f), (0.620000005f), 3, (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_2 = ___0_b;
		NullCheck(L_2);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_2, (0.0f), (-0.300000012f), (0.0f), (0.5f), (0.0399999991f), (0.5f), 5, (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_3 = ___0_b;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_4;
		L_4 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.300000012f), (0.0199999996f), (0.0f), NULL);
		NullCheck(L_3);
		Builder_Wing_m4000246F5FF33DAB01DA6B33EB1C083BB206263C(L_3, L_4, (-1.0f), 2, (0.460000008f), (0.419999987f), (0.0299999993f), (10.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_5 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6;
		L_6 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.300000012f), (0.0199999996f), (0.0f), NULL);
		NullCheck(L_5);
		Builder_Wing_m4000246F5FF33DAB01DA6B33EB1C083BB206263C(L_5, L_6, (1.0f), 2, (0.460000008f), (0.419999987f), (0.0299999993f), (10.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_7 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8;
		L_8 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0799999982f), (0.0500000007f), (0.300000012f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_9;
		L_9 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.119999997f), (0.100000001f), (0.419999987f), NULL);
		NullCheck(L_7);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_7, L_8, L_9, (0.0179999992f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_10 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_11;
		L_11 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.119999997f), (0.100000001f), (0.419999987f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12;
		L_12 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.25f), (0.200000003f), (1.0f), NULL);
		NullCheck(L_10);
		Builder_Dish_mCA47CAB74FD0464BEEF7DF5630E487EC9CAD4A91(L_10, L_11, L_12, (0.200000003f), (0.319999993f), ((int32_t)9), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_13 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_14;
		L_14 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.119999997f), (0.100000001f), (0.419999987f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_15;
		L_15 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.150000006f), (0.119999997f), (0.560000002f), NULL);
		NullCheck(L_13);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_13, L_14, L_15, (0.00999999978f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_16 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_17;
		L_17 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.200000003f), (0.319999993f), (-0.200000003f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_18;
		L_18 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.25f), (0.850000024f), (-0.219999999f), NULL);
		NullCheck(L_16);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_16, L_17, L_18, (0.0109999999f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_19 = ___0_b;
		NullCheck(L_19);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_19, (-0.150000006f), (-0.100000001f), (0.310000002f), (0.140000001f), (0.140000001f), (0.0299999993f), 8, (0.0f), NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Weather_mC8ADB2608AE61D9261571176769AD0165EDA58E4 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_0 = ___0_b;
		NullCheck(L_0);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_0, (0.0f), (0.0f), (0.0f), (0.560000002f), (0.600000024f), (0.560000002f), 3, (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_1 = ___0_b;
		NullCheck(L_1);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_1, (0.0f), (-0.159999996f), (0.0f), (0.579999983f), (0.300000012f), (0.579999983f), 0, (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_2 = ___0_b;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_3;
		L_3 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0399999991f), (0.300000012f), (0.0399999991f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_4;
		L_4 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0399999991f), (0.620000005f), (0.0399999991f), NULL);
		NullCheck(L_2);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_2, L_3, L_4, (0.170000002f), 4, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_5 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6;
		L_6 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0399999991f), (0.5f), (0.0399999991f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_7;
		L_7 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0399999991f), (0.5f), (0.300000012f), NULL);
		NullCheck(L_5);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_5, L_6, L_7, (0.0900000036f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_8 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_9;
		L_9 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0399999991f), (0.5f), (0.300000012f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10;
		L_10 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0399999991f), (0.5f), (0.319999993f), NULL);
		NullCheck(L_8);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_8, L_9, L_10, (0.075000003f), 8, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_11 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12;
		L_12 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0399999991f), (0.5f), (0.319999993f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_13;
		L_13 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0399999991f), (0.5f), (0.419999987f), NULL);
		NullCheck(L_11);
		Builder_Cone_mAC63A28D0AAFE35F676E5A01469B36833304726E(L_11, L_12, (0.0900000036f), L_13, (0.129999995f), 0, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_14 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_15;
		L_15 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.280000001f), (0.0f), (0.0f), NULL);
		NullCheck(L_14);
		Builder_Wing_m4000246F5FF33DAB01DA6B33EB1C083BB206263C(L_14, L_15, (-1.0f), 3, (0.400000006f), (0.479999989f), (0.0299999993f), (25.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_16 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_17;
		L_17 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.280000001f), (0.200000003f), (0.100000001f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_18;
		L_18 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((1.0f), (0.400000006f), (0.400000006f), NULL);
		NullCheck(L_16);
		Builder_Dish_mCA47CAB74FD0464BEEF7DF5630E487EC9CAD4A91(L_16, L_17, L_18, (0.140000001f), (0.219999999f), ((int32_t)9), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_19 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_20;
		L_20 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.200000003f), (-0.310000002f), (0.200000003f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_21;
		L_21 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.200000003f), (-0.550000012f), (0.239999995f), NULL);
		NullCheck(L_19);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_19, L_20, L_21, (0.00999999978f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_22 = ___0_b;
		NullCheck(L_22);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_22, (0.289999992f), (-0.0500000007f), (-0.100000001f), (0.0199999996f), (0.239999995f), (0.200000003f), 7, (0.0f), NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Comms_m2CE72A55FEB0AF6B819EAB02732355BA6DD6862F (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_0 = ___0_b;
		NullCheck(L_0);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_0, (0.0f), (0.0f), (0.0f), (0.620000005f), (0.779999971f), (0.600000024f), 0, (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_1 = ___0_b;
		NullCheck(L_1);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_1, (0.0f), (0.419999987f), (0.0f), (0.639999986f), (0.0799999982f), (0.620000005f), 3, (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_2 = ___0_b;
		NullCheck(L_2);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_2, (0.0f), (0.25f), (0.305000007f), (0.5f), (0.200000003f), (0.0199999996f), 3, (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_3 = ___0_b;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_4;
		L_4 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.310000002f), (0.0500000007f), (0.0f), NULL);
		NullCheck(L_3);
		Builder_Wing_m4000246F5FF33DAB01DA6B33EB1C083BB206263C(L_3, L_4, (-1.0f), 3, (0.419999987f), (0.560000002f), (0.0299999993f), (8.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_5 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6;
		L_6 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.310000002f), (0.0500000007f), (0.0f), NULL);
		NullCheck(L_5);
		Builder_Wing_m4000246F5FF33DAB01DA6B33EB1C083BB206263C(L_5, L_6, (1.0f), 3, (0.419999987f), (0.560000002f), (0.0299999993f), (8.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_7 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8;
		L_8 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.200000003f), (-0.0500000007f), (0.319999993f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_9;
		L_9 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.449999988f), (0.0500000007f), (1.0f), NULL);
		NullCheck(L_7);
		Builder_Dish_mCA47CAB74FD0464BEEF7DF5630E487EC9CAD4A91(L_7, L_8, L_9, (0.300000012f), (0.479999989f), ((int32_t)9), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_10 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_11;
		L_11 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.200000003f), (0.0f), (0.319999993f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12;
		L_12 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.449999988f), (0.200000003f), (1.0f), NULL);
		NullCheck(L_10);
		Builder_Dish_mCA47CAB74FD0464BEEF7DF5630E487EC9CAD4A91(L_10, L_11, L_12, (0.270000011f), (0.439999998f), ((int32_t)9), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_13 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_14;
		L_14 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.0799999982f), (0.319999993f), (0.310000002f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_15;
		L_15 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.239999995f), (0.180000007f), (0.550000012f), NULL);
		NullCheck(L_13);
		Builder_Cone_mAC63A28D0AAFE35F676E5A01469B36833304726E(L_13, L_14, (0.0299999993f), L_15, (0.0500000007f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_16 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_17;
		L_17 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0799999982f), (0.330000013f), (0.310000002f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_18;
		L_18 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.25999999f), (0.239999995f), (0.519999981f), NULL);
		NullCheck(L_16);
		Builder_Cone_mAC63A28D0AAFE35F676E5A01469B36833304726E(L_16, L_17, (0.0299999993f), L_18, (0.0450000018f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_19 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_20;
		L_20 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.460000008f), (0.100000001f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_21;
		L_21 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.100000001f), (1.0f), (0.300000012f), NULL);
		NullCheck(L_19);
		Builder_Dish_mCA47CAB74FD0464BEEF7DF5630E487EC9CAD4A91(L_19, L_20, L_21, (0.129999995f), (0.200000003f), ((int32_t)9), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_22 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_23;
		L_23 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.200000003f), (0.460000008f), (-0.200000003f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_24;
		L_24 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.219999999f), (0.800000012f), (-0.219999999f), NULL);
		NullCheck(L_22);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_22, L_23, L_24, (0.00999999978f), 5, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Telescope_m9E7DE068E58F019E17C7B5EE9B77EEBF98C130DD (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_0 = ___0_b;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_1;
		L_1 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.349999994f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2;
		L_2 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.75f), (0.0f), NULL);
		NullCheck(L_0);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_0, L_1, L_2, (0.300000012f), 1, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_3 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_4;
		L_4 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.745000005f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_5;
		L_5 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.754999995f), (0.0f), NULL);
		NullCheck(L_3);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_3, L_4, L_5, (0.270000011f), 7, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_6 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_7;
		L_7 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.25f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8;
		L_8 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.300000012f), (0.0f), NULL);
		NullCheck(L_6);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_6, L_7, L_8, (0.305000007f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_9 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10;
		L_10 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.720000029f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_11;
		L_11 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.349999994f), (0.0f), NULL);
		NullCheck(L_9);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_9, L_10, L_11, (0.330000013f), 0, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_12 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_13;
		L_13 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.75999999f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_14;
		L_14 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.720000029f), (0.0f), NULL);
		NullCheck(L_12);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_12, L_13, L_14, (0.25f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_15 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_16;
		L_16 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.949999988f), (-0.200000003f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_17;
		L_17 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.349999994f), (1.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_18;
		L_18 = V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892(L_17, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_19;
		L_19 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (1.0f), (0.0f), NULL);
		NullCheck(L_15);
		Builder_Slab_mAA7D3ABE211E7F908653A28B993A9888D6E4C9BE(L_15, L_16, L_18, L_19, (0.600000024f), (0.439999998f), (0.0299999993f), 4, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_20 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_21;
		L_21 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.75999999f), (-0.280000001f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_22;
		L_22 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.779999971f), (-0.270000011f), NULL);
		NullCheck(L_20);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_20, L_21, L_22, (0.0299999993f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_23 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_24;
		L_24 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.300000012f), (-0.200000003f), (0.0f), NULL);
		NullCheck(L_23);
		Builder_Wing_m4000246F5FF33DAB01DA6B33EB1C083BB206263C(L_23, L_24, (-1.0f), 2, (0.340000004f), (0.699999988f), (0.0250000004f), (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_25 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_26;
		L_26 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.300000012f), (-0.200000003f), (0.0f), NULL);
		NullCheck(L_25);
		Builder_Wing_m4000246F5FF33DAB01DA6B33EB1C083BB206263C(L_25, L_26, (1.0f), 2, (0.340000004f), (0.699999988f), (0.0250000004f), (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_27 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_28;
		L_28 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.200000003f), (-0.600000024f), (0.25f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_29;
		L_29 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.349999994f), (-0.699999988f), (0.5f), NULL);
		NullCheck(L_27);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_27, L_28, L_29, (0.0120000001f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_30 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_31;
		L_31 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.349999994f), (-0.699999988f), (0.5f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_32;
		L_32 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.300000012f), (-0.200000003f), (1.0f), NULL);
		NullCheck(L_30);
		Builder_Dish_mCA47CAB74FD0464BEEF7DF5630E487EC9CAD4A91(L_30, L_31, L_32, (0.119999997f), (0.180000007f), ((int32_t)9), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_33 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_34;
		L_34 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.200000003f), (-0.600000024f), (0.25f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_35;
		L_35 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.349999994f), (-0.75f), (0.449999988f), NULL);
		NullCheck(L_33);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_33, L_34, L_35, (0.0120000001f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_36 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_37;
		L_37 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.349999994f), (-0.75f), (0.449999988f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_38;
		L_38 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.400000006f), (-0.200000003f), (1.0f), NULL);
		NullCheck(L_36);
		Builder_Dish_mCA47CAB74FD0464BEEF7DF5630E487EC9CAD4A91(L_36, L_37, L_38, (0.109999999f), (0.170000002f), ((int32_t)9), NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Probe_m6FB885F57F94D43D6769B849495A52BBB1F34B17 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	float V_1 = 0.0f;
	int32_t V_2 = 0;
	float V_3 = 0.0f;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_4;
	memset((&V_4), 0, sizeof(V_4));
	{
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_0 = ___0_b;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_1;
		L_1 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.219999999f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2;
		L_2 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.0799999982f), (0.0f), NULL);
		NullCheck(L_0);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_0, L_1, L_2, (0.300000012f), 0, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_3 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_4;
		L_4 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.0799999982f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_5;
		L_5 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.119999997f), (0.0f), NULL);
		NullCheck(L_3);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_3, L_4, L_5, (0.239999995f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_6 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_7;
		L_7 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.119999997f), (0.0199999996f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8;
		L_8 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (1.0f), (0.300000012f), NULL);
		NullCheck(L_6);
		Builder_Dish_mCA47CAB74FD0464BEEF7DF5630E487EC9CAD4A91(L_6, L_7, L_8, (0.620000005f), (0.850000024f), ((int32_t)9), NULL);
		V_0 = 0;
		goto IL_010e;
	}

IL_00a6:
	{
		int32_t L_9 = V_0;
		V_1 = ((float)il2cpp_codegen_multiply(((float)L_9), (2.09439993f)));
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_10 = ___0_b;
		float L_11 = V_1;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_12;
		L_12 = cos(((double)L_11));
		float L_13 = V_1;
		double L_14;
		L_14 = sin(((double)L_13));
		float L_15 = V_1;
		double L_16;
		L_16 = sin(((double)L_15));
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_17;
		L_17 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1(((float)il2cpp_codegen_multiply(((float)L_12), (0.449999988f))), ((float)il2cpp_codegen_add((0.239999995f), ((float)il2cpp_codegen_multiply(((float)L_14), (0.119999997f))))), ((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(((float)L_16), (0.400000006f))), (0.0599999987f))), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_18;
		L_18 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.600000024f), (0.170000002f), NULL);
		NullCheck(L_10);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_10, L_17, L_18, (0.00800000038f), 5, NULL);
		int32_t L_19 = V_0;
		V_0 = ((int32_t)il2cpp_codegen_add(L_19, 1));
	}

IL_010e:
	{
		int32_t L_20 = V_0;
		if ((((int32_t)L_20) < ((int32_t)3)))
		{
			goto IL_00a6;
		}
	}
	{
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_21 = ___0_b;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_22;
		L_22 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.550000012f), (0.150000006f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_23;
		L_23 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.660000026f), (0.189999998f), NULL);
		NullCheck(L_21);
		Builder_Cone_mAC63A28D0AAFE35F676E5A01469B36833304726E(L_21, L_22, (0.0399999991f), L_23, (0.0700000003f), 6, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_24 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_25;
		L_25 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.25f), (-0.100000001f), (0.100000001f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_26;
		L_26 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.949999988f), (-0.319999993f), (0.300000012f), NULL);
		NullCheck(L_24);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_24, L_25, L_26, (0.0160000008f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_27 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_28;
		L_28 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.879999995f), (-0.300000012f), (0.280000001f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_29;
		L_29 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((1.12f), (-0.379999995f), (0.349999994f), NULL);
		NullCheck(L_27);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_27, L_28, L_29, (0.0700000003f), 7, NULL);
		V_2 = 0;
		goto IL_0259;
	}

IL_01ba:
	{
		int32_t L_30 = V_2;
		V_3 = ((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(((float)L_30), (1.57079995f))), (0.400000006f)));
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_31;
		L_31 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((1.0f), (-0.340000004f), (0.314999998f), NULL);
		V_4 = L_31;
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_32 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_33 = V_4;
		float L_34 = V_3;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_35;
		L_35 = cos(((double)L_34));
		float L_36 = V_3;
		double L_37;
		L_37 = sin(((double)L_36));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_38;
		L_38 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), ((float)il2cpp_codegen_multiply(((float)L_35), (0.0900000036f))), ((float)il2cpp_codegen_multiply(((float)L_37), (0.0900000036f))), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_39;
		L_39 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_33, L_38, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_40;
		L_40 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.949999988f), (-0.300000012f), (0.280000001f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_41;
		L_41 = V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892(L_40, NULL);
		float L_42 = V_3;
		double L_43;
		L_43 = cos(((double)L_42));
		float L_44 = V_3;
		double L_45;
		L_45 = sin(((double)L_44));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_46;
		L_46 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), ((float)L_43), ((float)L_45), NULL);
		NullCheck(L_32);
		Builder_Slab_mAA7D3ABE211E7F908653A28B993A9888D6E4C9BE(L_32, L_39, L_41, L_46, (0.0120000001f), (0.0799999982f), (0.219999999f), 7, NULL);
		int32_t L_47 = V_2;
		V_2 = ((int32_t)il2cpp_codegen_add(L_47, 1));
	}

IL_0259:
	{
		int32_t L_48 = V_2;
		if ((((int32_t)L_48) < ((int32_t)4)))
		{
			goto IL_01ba;
		}
	}
	{
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_49 = ___0_b;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_50;
		L_50 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.25f), (-0.100000001f), (-0.0500000007f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_51;
		L_51 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-1.04999995f), (-0.0199999996f), (-0.300000012f), NULL);
		NullCheck(L_49);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_49, L_50, L_51, (0.00700000022f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_52 = ___0_b;
		NullCheck(L_52);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_52, (-0.180000007f), (-0.280000001f), (0.280000001f), (0.159999996f), (0.119999997f), (0.119999997f), 3, (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_53 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_54;
		L_54 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.180000007f), (-0.280000001f), (0.340000004f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_55;
		L_55 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.180000007f), (-0.280000001f), (0.379999995f), NULL);
		NullCheck(L_53);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_53, L_54, L_55, (0.0350000001f), 8, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Cargo_m02A2F4319102F01B39154A358AC3E2A3DFD476C2 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_0 = ___0_b;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_1;
		L_1 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.25f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2;
		L_2 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.349999994f), (0.0f), NULL);
		NullCheck(L_0);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_0, L_1, L_2, (0.379999995f), 4, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_3 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_4;
		L_4 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.349999994f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_5;
		L_5 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.620000005f), (0.0f), NULL);
		NullCheck(L_3);
		Builder_Cone_mAC63A28D0AAFE35F676E5A01469B36833304726E(L_3, L_4, (0.379999995f), L_5, (0.200000003f), 4, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_6 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_7;
		L_7 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.620000005f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8;
		L_8 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.699999988f), (0.0f), NULL);
		NullCheck(L_6);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_6, L_7, L_8, (0.150000006f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_9 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10;
		L_10 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.699999988f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_11;
		L_11 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.720000029f), (0.0f), NULL);
		NullCheck(L_9);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_9, L_10, L_11, (0.170000002f), 6, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_12 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_13;
		L_13 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.300000012f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_14;
		L_14 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.25f), (0.0f), NULL);
		NullCheck(L_12);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_12, L_13, L_14, (0.395000011f), 0, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_15 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_16;
		L_16 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.850000024f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_17;
		L_17 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.300000012f), (0.0f), NULL);
		NullCheck(L_15);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_15, L_16, L_17, (0.400000006f), 2, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_18 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_19;
		L_19 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.5f), (-0.600000024f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_20;
		L_20 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.0f), (1.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_21;
		L_21 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (1.0f), (0.0f), NULL);
		NullCheck(L_18);
		Builder_Slab_mAA7D3ABE211E7F908653A28B993A9888D6E4C9BE(L_18, L_19, L_20, L_21, (0.219999999f), (0.400000006f), (0.0199999996f), 4, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_22 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_23;
		L_23 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.5f), (-0.600000024f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_24;
		L_24 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.0f), (1.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_25;
		L_25 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (1.0f), (0.0f), NULL);
		NullCheck(L_22);
		Builder_Slab_mAA7D3ABE211E7F908653A28B993A9888D6E4C9BE(L_22, L_23, L_24, L_25, (0.219999999f), (0.400000006f), (0.0199999996f), 4, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_26 = ___0_b;
		NullCheck(L_26);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_26, (0.100000001f), (0.0799999982f), (0.370000005f), (0.159999996f), (0.200000003f), (0.0299999993f), 7, (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_27 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_28;
		L_28 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.159999996f), (0.200000003f), (0.340000004f), NULL);
		NullCheck(L_27);
		Builder_Ball_m2926A9ADD07DBA2D6A030098084DB1CB63778121(L_27, L_28, (0.0450000018f), 8, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Crew_m88C2F633AE6D651D7E02A5F3BA86B2491372A84D (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_0 = ___0_b;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_1;
		L_1 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.25f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2;
		L_2 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.349999994f), (0.0f), NULL);
		NullCheck(L_0);
		Builder_Cone_mAC63A28D0AAFE35F676E5A01469B36833304726E(L_0, L_1, (0.479999989f), L_2, (0.209999993f), 4, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_3 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_4;
		L_4 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.300000012f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_5;
		L_5 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.25f), (0.0f), NULL);
		NullCheck(L_3);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_3, L_4, L_5, (0.49000001f), 7, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_6 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_7;
		L_7 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.349999994f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8;
		L_8 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.449999988f), (0.0f), NULL);
		NullCheck(L_6);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_6, L_7, L_8, (0.129999995f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_9 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10;
		L_10 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.449999988f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_11;
		L_11 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.469999999f), (0.0f), NULL);
		NullCheck(L_9);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_9, L_10, L_11, (0.150000006f), 6, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_12 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_13;
		L_13 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.129999995f), (0.119999997f), (0.289999992f), NULL);
		NullCheck(L_12);
		Builder_Ball_m2926A9ADD07DBA2D6A030098084DB1CB63778121(L_12, L_13, (0.0500000007f), 8, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_14 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_15;
		L_15 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.129999995f), (0.119999997f), (0.289999992f), NULL);
		NullCheck(L_14);
		Builder_Ball_m2926A9ADD07DBA2D6A030098084DB1CB63778121(L_14, L_15, (0.0500000007f), 8, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_16 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_17;
		L_17 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.310000002f), (0.0f), (0.119999997f), NULL);
		NullCheck(L_16);
		Builder_Ball_m2926A9ADD07DBA2D6A030098084DB1CB63778121(L_16, L_17, (0.0450000018f), 8, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_18 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_19;
		L_19 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.800000012f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_20;
		L_20 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.300000012f), (0.0f), NULL);
		NullCheck(L_18);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_18, L_19, L_20, (0.469999999f), 2, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_21 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_22;
		L_22 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.550000012f), (-0.620000005f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_23;
		L_23 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.0f), (1.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_24;
		L_24 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (1.0f), (0.0f), NULL);
		NullCheck(L_21);
		Builder_Slab_mAA7D3ABE211E7F908653A28B993A9888D6E4C9BE(L_21, L_22, L_23, L_24, (0.180000007f), (0.319999993f), (0.0199999996f), 4, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_25 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_26;
		L_26 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.550000012f), (-0.620000005f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_27;
		L_27 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.0f), (1.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_28;
		L_28 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (1.0f), (0.0f), NULL);
		NullCheck(L_25);
		Builder_Slab_mAA7D3ABE211E7F908653A28B993A9888D6E4C9BE(L_25, L_26, L_27, L_28, (0.180000007f), (0.319999993f), (0.0199999996f), 4, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_29 = ___0_b;
		NullCheck(L_29);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_29, (0.0f), (-0.0500000007f), (0.409999996f), (0.119999997f), (0.159999996f), (0.0299999993f), 6, (0.0f), NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Dispenser_mB71E6312D962D8722CCC8D2AF8D6DAE85176648F (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	float V_1 = 0.0f;
	float V_2 = 0.0f;
	float G_B4_0 = 0.0f;
	float G_B7_0 = 0.0f;
	{
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_0 = ___0_b;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_1;
		L_1 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.620000005f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2;
		L_2 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.5f), (0.0f), NULL);
		NullCheck(L_0);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_0, L_1, L_2, (0.419999987f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_3 = ___0_b;
		NullCheck(L_3);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_3, (0.0f), (-0.449999988f), (0.0f), (0.920000017f), (0.0799999982f), (0.920000017f), 0, (0.0f), NULL);
		V_0 = 0;
		goto IL_016e;
	}

IL_0065:
	{
		int32_t L_4 = V_0;
		if (!((int32_t)(L_4&1)))
		{
			goto IL_0071;
		}
	}
	{
		G_B4_0 = (0.219999999f);
		goto IL_0076;
	}

IL_0071:
	{
		G_B4_0 = (-0.219999999f);
	}

IL_0076:
	{
		V_1 = G_B4_0;
		int32_t L_5 = V_0;
		if (!((int32_t)(L_5&2)))
		{
			goto IL_0083;
		}
	}
	{
		G_B7_0 = (0.219999999f);
		goto IL_0088;
	}

IL_0083:
	{
		G_B7_0 = (-0.219999999f);
	}

IL_0088:
	{
		V_2 = G_B7_0;
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_6 = ___0_b;
		float L_7 = V_1;
		float L_8 = V_2;
		NullCheck(L_6);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_6, L_7, (-0.0599999987f), L_8, (0.360000014f), (0.699999988f), (0.360000014f), 3, (0.0f), NULL);
		float L_9 = V_2;
		if ((!(((float)L_9) > ((float)(0.0f)))))
		{
			goto IL_0118;
		}
	}
	{
		float L_10 = V_1;
		if ((!(((float)L_10) < ((float)(0.0f)))))
		{
			goto IL_0118;
		}
	}
	{
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_11 = ___0_b;
		float L_12 = V_1;
		float L_13 = V_2;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_14;
		L_14 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1(((float)il2cpp_codegen_subtract(L_12, (0.100000001f))), (-0.0599999987f), ((float)il2cpp_codegen_add(L_13, (0.300000012f))), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_15;
		L_15 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-1.0f), (0.0f), (1.20000005f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_16;
		L_16 = V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892(L_15, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_17;
		L_17 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (1.0f), (0.0f), NULL);
		NullCheck(L_11);
		Builder_Slab_mAA7D3ABE211E7F908653A28B993A9888D6E4C9BE(L_11, L_14, L_16, L_17, (0.319999993f), (0.620000005f), (0.0199999996f), 6, NULL);
		goto IL_0148;
	}

IL_0118:
	{
		float L_18 = V_2;
		if ((!(((float)L_18) > ((float)(0.0f)))))
		{
			goto IL_0148;
		}
	}
	{
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_19 = ___0_b;
		float L_20 = V_1;
		float L_21 = V_2;
		NullCheck(L_19);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_19, L_20, (-0.0599999987f), ((float)il2cpp_codegen_add(L_21, (0.189999998f))), (0.319999993f), (0.620000005f), (0.0199999996f), 6, (0.0f), NULL);
	}

IL_0148:
	{
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_22 = ___0_b;
		float L_23 = V_1;
		float L_24 = V_2;
		NullCheck(L_22);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_22, L_23, (0.300000012f), L_24, (0.370000005f), (0.0299999993f), (0.370000005f), 5, (0.0f), NULL);
		int32_t L_25 = V_0;
		V_0 = ((int32_t)il2cpp_codegen_add(L_25, 1));
	}

IL_016e:
	{
		int32_t L_26 = V_0;
		if ((((int32_t)L_26) < ((int32_t)4)))
		{
			goto IL_0065;
		}
	}
	{
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_27 = ___0_b;
		NullCheck(L_27);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_27, (-0.219999999f), (-0.0399999991f), (0.400000006f), (0.200000003f), (0.200000003f), (0.200000003f), 2, (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_28 = ___0_b;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_29;
		L_29 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.439999998f), (-0.400000006f), (0.439999998f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_30;
		L_30 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.439999998f), (0.319999993f), (0.439999998f), NULL);
		NullCheck(L_28);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_28, L_29, L_30, (0.0149999997f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_31 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_32;
		L_32 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.439999998f), (-0.400000006f), (-0.439999998f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_33;
		L_33 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.439999998f), (0.319999993f), (-0.439999998f), NULL);
		NullCheck(L_31);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_31, L_32, L_33, (0.0149999997f), 5, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Relay_mB44BDFC48EEAE2E7A17D5E6156A4997516EC1304 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_0 = ___0_b;
		NullCheck(L_0);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_0, (0.0f), (0.0f), (0.0f), (0.5f), (0.5f), (0.5f), 0, (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_1 = ___0_b;
		NullCheck(L_1);
		Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5(L_1, (0.0f), (0.270000011f), (0.0f), (0.519999981f), (0.0399999991f), (0.519999981f), 3, (0.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_2 = ___0_b;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_3;
		L_3 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.289999992f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_4;
		L_4 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.419999987f), (0.0500000007f), NULL);
		NullCheck(L_2);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_2, L_3, L_4, (0.0299999993f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_5 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6;
		L_6 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.419999987f), (0.0500000007f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_7;
		L_7 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.150000006f), (0.800000012f), (0.600000024f), NULL);
		NullCheck(L_5);
		Builder_Dish_mCA47CAB74FD0464BEEF7DF5630E487EC9CAD4A91(L_5, L_6, L_7, (0.550000012f), (0.75f), ((int32_t)9), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_8 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_9;
		L_9 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.200000003f), (0.449999988f), (0.300000012f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10;
		L_10 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0799999982f), (0.819999993f), (0.379999995f), NULL);
		NullCheck(L_8);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_8, L_9, L_10, (0.00800000038f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_11 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12;
		L_12 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.200000003f), (0.449999988f), (0.300000012f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_13;
		L_13 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0799999982f), (0.819999993f), (0.379999995f), NULL);
		NullCheck(L_11);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_11, L_12, L_13, (0.00800000038f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_14 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_15;
		L_15 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0799999982f), (0.800000012f), (0.370000005f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_16;
		L_16 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0599999987f), (0.720000029f), (0.330000013f), NULL);
		NullCheck(L_14);
		Builder_Cone_mAC63A28D0AAFE35F676E5A01469B36833304726E(L_14, L_15, (0.0299999993f), L_16, (0.0599999987f), 6, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_17 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_18;
		L_18 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.25f), (-0.0199999996f), (0.0f), NULL);
		NullCheck(L_17);
		Builder_Wing_m4000246F5FF33DAB01DA6B33EB1C083BB206263C(L_17, L_18, (-1.0f), 2, (0.400000006f), (0.400000006f), (0.0299999993f), (15.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_19 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_20;
		L_20 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.25f), (-0.0199999996f), (0.0f), NULL);
		NullCheck(L_19);
		Builder_Wing_m4000246F5FF33DAB01DA6B33EB1C083BB206263C(L_19, L_20, (1.0f), 2, (0.400000006f), (0.400000006f), (0.0299999993f), (15.0f), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_21 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_22;
		L_22 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.200000003f), (-0.25f), (0.200000003f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_23;
		L_23 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.300000012f), (-0.699999988f), (0.300000012f), NULL);
		NullCheck(L_21);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_21, L_22, L_23, (0.00899999961f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_24 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_25;
		L_25 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.200000003f), (-0.25f), (0.200000003f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_26;
		L_26 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.319999993f), (-0.699999988f), (0.280000001f), NULL);
		NullCheck(L_24);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_24, L_25, L_26, (0.00899999961f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_27 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_28;
		L_28 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.200000003f), (-0.25f), (-0.200000003f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_29;
		L_29 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.300000012f), (-0.680000007f), (-0.300000012f), NULL);
		NullCheck(L_27);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_27, L_28, L_29, (0.00899999961f), 5, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender_Lander_m0FCDC40AF68DF2D14FE60B4452A077ED07BB9827 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* ___0_b, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	float V_1 = 0.0f;
	float V_2 = 0.0f;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_3;
	memset((&V_3), 0, sizeof(V_3));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_4;
	memset((&V_4), 0, sizeof(V_4));
	{
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_0 = ___0_b;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_1;
		L_1 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.150000006f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2;
		L_2 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.200000003f), (0.0f), NULL);
		NullCheck(L_0);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_0, L_1, L_2, (0.400000006f), 0, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_3 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_4;
		L_4 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.200000003f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_5;
		L_5 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.239999995f), (0.0f), NULL);
		NullCheck(L_3);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_3, L_4, L_5, (0.419999987f), 3, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_6 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_7;
		L_7 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((-0.100000001f), (0.270000011f), (0.0199999996f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8;
		L_8 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (1.0f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_9;
		L_9 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.0f), (-1.0f), NULL);
		NullCheck(L_6);
		Builder_Slab_mAA7D3ABE211E7F908653A28B993A9888D6E4C9BE(L_6, L_7, L_8, L_9, (0.400000006f), (0.300000012f), (0.0199999996f), 2, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_10 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_11;
		L_11 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.239999995f), (0.319999993f), (-0.119999997f), NULL);
		NullCheck(L_10);
		Builder_Ball_m2926A9ADD07DBA2D6A030098084DB1CB63778121(L_10, L_11, (0.109999999f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_12 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_13;
		L_13 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.159999996f), (0.239999995f), (0.180000007f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_14;
		L_14 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.159999996f), (0.379999995f), (0.200000003f), NULL);
		NullCheck(L_12);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_12, L_13, L_14, (0.0120000001f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_15 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_16;
		L_16 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.159999996f), (0.379999995f), (0.200000003f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_17;
		L_17 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.300000012f), (1.0f), (0.5f), NULL);
		NullCheck(L_15);
		Builder_Dish_mCA47CAB74FD0464BEEF7DF5630E487EC9CAD4A91(L_15, L_16, L_17, (0.100000001f), (0.159999996f), ((int32_t)9), NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_18 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_19;
		L_19 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.150000006f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_20;
		L_20 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.379999995f), (0.0f), NULL);
		NullCheck(L_18);
		Builder_Cone_mAC63A28D0AAFE35F676E5A01469B36833304726E(L_18, L_19, (0.0799999982f), L_20, (0.170000002f), 7, NULL);
		V_0 = 0;
		goto IL_0278;
	}

IL_0188:
	{
		int32_t L_21 = V_0;
		float L_22 = ((float)il2cpp_codegen_add((0.785399973f), ((float)il2cpp_codegen_multiply(((float)L_21), (1.57079995f)))));
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_23;
		L_23 = cos(((double)L_22));
		V_1 = ((float)L_23);
		double L_24;
		L_24 = sin(((double)L_22));
		V_2 = ((float)L_24);
		float L_25 = V_1;
		float L_26 = V_2;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_27;
		L_27 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1(((float)il2cpp_codegen_multiply(L_25, (0.360000014f))), (-0.0799999982f), ((float)il2cpp_codegen_multiply(L_26, (0.360000014f))), NULL);
		V_3 = L_27;
		float L_28 = V_1;
		float L_29 = V_2;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_30;
		L_30 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1(((float)il2cpp_codegen_multiply(L_28, (0.699999988f))), (-0.579999983f), ((float)il2cpp_codegen_multiply(L_29, (0.699999988f))), NULL);
		V_4 = L_30;
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_31 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_32 = V_3;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_33 = V_4;
		NullCheck(L_31);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_31, L_32, L_33, (0.0219999999f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_34 = ___0_b;
		float L_35 = V_1;
		float L_36 = V_2;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_37;
		L_37 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1(((float)il2cpp_codegen_multiply(L_35, (0.300000012f))), (0.119999997f), ((float)il2cpp_codegen_multiply(L_36, (0.300000012f))), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_38 = V_4;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_39;
		L_39 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.119999997f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_40;
		L_40 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_39, (0.5f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_41;
		L_41 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_38, L_40, NULL);
		NullCheck(L_34);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_34, L_37, L_41, (0.0120000001f), 5, NULL);
		Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* L_42 = ___0_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_43 = V_4;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_44;
		L_44 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (-0.0199999996f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_45;
		L_45 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_43, L_44, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_46 = V_4;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_47;
		L_47 = PayloadRender_P_mADA636C45477FD0B090A8EC9E8A58572135823A1((0.0f), (0.0199999996f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_48;
		L_48 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_46, L_47, NULL);
		NullCheck(L_42);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(L_42, L_45, L_48, (0.0900000036f), 0, NULL);
		int32_t L_49 = V_0;
		V_0 = ((int32_t)il2cpp_codegen_add(L_49, 1));
	}

IL_0278:
	{
		int32_t L_50 = V_0;
		if ((((int32_t)L_50) < ((int32_t)4)))
		{
			goto IL_0188;
		}
	}
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint8_t PayloadRender_Byte_m6D17F208B2A40D9E9DEDE0C4D5872C416E674974 (float ___0_v, const RuntimeMethod* method) 
{
	int32_t V_0 = 0;
	int32_t G_B5_0 = 0;
	{
		float L_0 = ___0_v;
		V_0 = il2cpp_codegen_cast_double_to_int<int32_t>(((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(L_0, (255.0f))), (0.5f))));
		int32_t L_1 = V_0;
		if ((((int32_t)L_1) < ((int32_t)0)))
		{
			goto IL_0025;
		}
	}
	{
		int32_t L_2 = V_0;
		if ((((int32_t)L_2) > ((int32_t)((int32_t)255))))
		{
			goto IL_001e;
		}
	}
	{
		int32_t L_3 = V_0;
		G_B5_0 = L_3;
		goto IL_0026;
	}

IL_001e:
	{
		G_B5_0 = ((int32_t)255);
		goto IL_0026;
	}

IL_0025:
	{
		G_B5_0 = 0;
	}

IL_0026:
	{
		return (uint8_t)((int32_t)(uint8_t)G_B5_0);
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t PayloadRender_Clamp_m031B01B75934395207D92A93B1208E9EA1905F81 (int32_t ___0_v, int32_t ___1_lo, int32_t ___2_hi, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = ___0_v;
		int32_t L_1 = ___1_lo;
		if ((((int32_t)L_0) < ((int32_t)L_1)))
		{
			goto IL_000c;
		}
	}
	{
		int32_t L_2 = ___0_v;
		int32_t L_3 = ___2_hi;
		if ((((int32_t)L_2) > ((int32_t)L_3)))
		{
			goto IL_000a;
		}
	}
	{
		int32_t L_4 = ___0_v;
		return L_4;
	}

IL_000a:
	{
		int32_t L_5 = ___2_hi;
		return L_5;
	}

IL_000c:
	{
		int32_t L_6 = ___1_lo;
		return L_6;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadRender_Frac_mCB86369BB619EE9BE740A60F04823BB2377D9BBF (float ___0_x, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		float L_0 = ___0_x;
		float L_1 = ___0_x;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_2;
		L_2 = floor(((double)L_1));
		return ((float)il2cpp_codegen_subtract(L_0, ((float)L_2)));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadRender_Pow_mA707E79B5D2EC2D61714F837291FBD2BFA7AEEA0 (float ___0_x, float ___1_p, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		float L_0 = ___0_x;
		if ((((float)L_0) <= ((float)(0.0f))))
		{
			goto IL_0013;
		}
	}
	{
		float L_1 = ___0_x;
		float L_2 = ___1_p;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_3;
		L_3 = Math_Pow_mEAE651F0858203FBE12B72B6A53951BBD0FB5265(((double)L_1), ((double)L_2), NULL);
		return ((float)L_3);
	}

IL_0013:
	{
		return (0.0f);
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PayloadRender_Smooth_m5380759CCAE4BC8D1592EBE7088C4AE973E5E9BE (float ___0_a, float ___1_b, float ___2_x, const RuntimeMethod* method) 
{
	float V_0 = 0.0f;
	float G_B5_0 = 0.0f;
	{
		float L_0 = ___2_x;
		float L_1 = ___0_a;
		float L_2 = ___1_b;
		float L_3 = ___0_a;
		V_0 = ((float)(((float)il2cpp_codegen_subtract(L_0, L_1))/((float)il2cpp_codegen_subtract(L_2, L_3))));
		float L_4 = V_0;
		if ((((float)L_4) < ((float)(0.0f))))
		{
			goto IL_0022;
		}
	}
	{
		float L_5 = V_0;
		if ((((float)L_5) > ((float)(1.0f))))
		{
			goto IL_001b;
		}
	}
	{
		float L_6 = V_0;
		G_B5_0 = L_6;
		goto IL_0027;
	}

IL_001b:
	{
		G_B5_0 = (1.0f);
		goto IL_0027;
	}

IL_0022:
	{
		G_B5_0 = (0.0f);
	}

IL_0027:
	{
		V_0 = G_B5_0;
		float L_7 = V_0;
		float L_8 = V_0;
		float L_9 = V_0;
		return ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_multiply(L_7, L_8)), ((float)il2cpp_codegen_subtract((3.0f), ((float)il2cpp_codegen_multiply((2.0f), L_9))))));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PayloadRender__cctor_m79A8BA1E405198F28EE500C0BDD3118FFB0FFF86 (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Dictionary_2__ctor_m432862A4D3846A8EC1E05271D1CA0346C288B93D_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&U3CPrivateImplementationDetailsU3E_t91B5382CE2346E184773DCFBD60B8EF7D20A968C____E780A4ABA35D7432F324407DF0FD7E0C6F361D1AD2FFC67E5B9F893DFDD80800_FieldInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* L_0 = (SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C*)(SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C*)SZArrayNew(SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C_il2cpp_TypeInfo_var, (uint32_t)8);
		SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* L_1 = L_0;
		RuntimeFieldHandle_t6E4C45B6D2EA12FC99185805A7E77527899B25C5 L_2 = { reinterpret_cast<intptr_t> (U3CPrivateImplementationDetailsU3E_t91B5382CE2346E184773DCFBD60B8EF7D20A968C____E780A4ABA35D7432F324407DF0FD7E0C6F361D1AD2FFC67E5B9F893DFDD80800_FieldInfo_var) };
		RuntimeHelpers_InitializeArray_m751372AA3F24FBF6DA9B9D687CBFA2DE436CAB9B((RuntimeArray*)L_1, L_2, NULL);
		((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___Rgss = L_1;
		Il2CppCodeGenWriteBarrier((void**)(&((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___Rgss), (void*)L_1);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_3;
		memset((&L_3), 0, sizeof(L_3));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_3), (1.0f), (0.620000005f), (1.29999995f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_4;
		L_4 = V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892(L_3, NULL);
		((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___ViewDir = L_4;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_5;
		memset((&L_5), 0, sizeof(L_5));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_5), (-0.550000012f), (0.850000024f), (0.649999976f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6;
		L_6 = V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892(L_5, NULL);
		((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___KeyDir = L_6;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_7;
		memset((&L_7), 0, sizeof(L_7));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_7), (0.649999976f), (0.300000012f), (-1.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8;
		L_8 = V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892(L_7, NULL);
		((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___RimDir = L_8;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_9;
		memset((&L_9), 0, sizeof(L_9));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_9), (1.04999995f), (0.970000029f), (0.860000014f), NULL);
		((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___KeyCol = L_9;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10;
		memset((&L_10), 0, sizeof(L_10));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_10), (0.419999987f), (0.620000005f), (1.0f), NULL);
		((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___RimCol = L_10;
		Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A* L_11 = (Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A*)il2cpp_codegen_object_new(Dictionary_2_t73A1C0036EF46438BA808FB7101E7AC425E4482A_il2cpp_TypeInfo_var);
		Dictionary_2__ctor_m432862A4D3846A8EC1E05271D1CA0346C288B93D(L_11, Dictionary_2__ctor_m432862A4D3846A8EC1E05271D1CA0346C288B93D_RuntimeMethod_var);
		((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___Scenes = L_11;
		Il2CppCodeGenWriteBarrier((void**)(&((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___Scenes), (void*)L_11);
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
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Job_get_Done_m16154739569DD8E47D867CA2FE93E82CCFAA9E22 (Job_t0B1165E35BF83E24991BB9F20783AADD55C95267* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->____row;
		int32_t L_1 = __this->___Px;
		return (bool)((((int32_t)((((int32_t)L_0) < ((int32_t)((int32_t)il2cpp_codegen_multiply(L_1, 2))))? 1 : 0)) == ((int32_t)0))? 1 : 0);
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Job__ctor_m89419F39EB941FDAA2D4CA1E62E8EE9C3A0FE7EE (Job_t0B1165E35BF83E24991BB9F20783AADD55C95267* __this, String_t* ___0_form, int32_t ___1_px, int32_t ___2_backdrop, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&C4U5BU5D_tA0B6DD53ECB5C3687C30C1AE88FBB09AA96AC22B_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral3C4F52AE03EA8FEF5DEAA4BF1DCF2792C970C821);
		s_Il2CppMethodInitialized = true;
	}
	Job_t0B1165E35BF83E24991BB9F20783AADD55C95267* G_B2_0 = NULL;
	Job_t0B1165E35BF83E24991BB9F20783AADD55C95267* G_B1_0 = NULL;
	String_t* G_B3_0 = NULL;
	Job_t0B1165E35BF83E24991BB9F20783AADD55C95267* G_B3_1 = NULL;
	{
		Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2(__this, NULL);
		int32_t L_0 = ___1_px;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		int32_t L_1;
		L_1 = PayloadRender_Clamp_m031B01B75934395207D92A93B1208E9EA1905F81(L_0, ((int32_t)16), ((int32_t)1024), NULL);
		__this->___Px = L_1;
		String_t* L_2 = ___0_form;
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		bool L_3;
		L_3 = PayloadForms_KnownForm_mF1438848D2B6822053BEC97A6A0E610587144811(L_2, NULL);
		if (L_3)
		{
			G_B2_0 = __this;
			goto IL_0029;
		}
		G_B1_0 = __this;
	}
	{
		G_B3_0 = _stringLiteral3C4F52AE03EA8FEF5DEAA4BF1DCF2792C970C821;
		G_B3_1 = G_B1_0;
		goto IL_002a;
	}

IL_0029:
	{
		String_t* L_4 = ___0_form;
		G_B3_0 = L_4;
		G_B3_1 = G_B2_0;
	}

IL_002a:
	{
		NullCheck(G_B3_1);
		G_B3_1->___Form = G_B3_0;
		Il2CppCodeGenWriteBarrier((void**)(&G_B3_1->___Form), (void*)G_B3_0);
		int32_t L_5 = ___2_backdrop;
		__this->____backdrop = L_5;
		String_t* L_6 = __this->___Form;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_7;
		L_7 = PayloadRender_Scene_mF202D14509424834B06B24444ADF5AC9BCD5B49A(L_6, NULL);
		__this->____scene = L_7;
		Il2CppCodeGenWriteBarrier((void**)(&__this->____scene), (void*)L_7);
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_8 = __this->____scene;
		Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_9;
		L_9 = PayloadRender_Frame_mACB603D0B07928A49BEDD4335DC3A98C01CB8196(L_8, NULL);
		__this->____cam = L_9;
		int32_t L_10 = __this->___Px;
		int32_t L_11 = __this->___Px;
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_12 = (Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C*)(Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C*)SZArrayNew(Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C_il2cpp_TypeInfo_var, (uint32_t)((int32_t)il2cpp_codegen_multiply(L_10, L_11)));
		__this->____ids = L_12;
		Il2CppCodeGenWriteBarrier((void**)(&__this->____ids), (void*)L_12);
		int32_t L_13 = __this->___Px;
		int32_t L_14 = __this->___Px;
		C4U5BU5D_tA0B6DD53ECB5C3687C30C1AE88FBB09AA96AC22B* L_15 = (C4U5BU5D_tA0B6DD53ECB5C3687C30C1AE88FBB09AA96AC22B*)(C4U5BU5D_tA0B6DD53ECB5C3687C30C1AE88FBB09AA96AC22B*)SZArrayNew(C4U5BU5D_tA0B6DD53ECB5C3687C30C1AE88FBB09AA96AC22B_il2cpp_TypeInfo_var, (uint32_t)((int32_t)il2cpp_codegen_multiply(L_13, L_14)));
		__this->____col = L_15;
		Il2CppCodeGenWriteBarrier((void**)(&__this->____col), (void*)L_15);
		int32_t L_16 = __this->___Px;
		int32_t L_17 = __this->___Px;
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_18 = (ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031*)(ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031*)SZArrayNew(ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031_il2cpp_TypeInfo_var, (uint32_t)((int32_t)il2cpp_codegen_multiply(((int32_t)il2cpp_codegen_multiply(L_16, L_17)), 4)));
		__this->___Pixels = L_18;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___Pixels), (void*)L_18);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Job_Step_mA354313D818FA7693E6076FBB886FB20772E675A (Job_t0B1165E35BF83E24991BB9F20783AADD55C95267* __this, int32_t ___0_rows, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	int32_t V_1 = 0;
	int32_t V_2 = 0;
	int32_t V_3 = 0;
	int32_t V_4 = 0;
	int32_t V_5 = 0;
	int32_t V_6 = 0;
	int32_t V_7 = 0;
	int32_t V_8 = 0;
	C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F V_9;
	memset((&V_9), 0, sizeof(V_9));
	int32_t V_10 = 0;
	int32_t V_11 = 0;
	C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F V_12;
	memset((&V_12), 0, sizeof(V_12));
	int32_t V_13 = 0;
	int32_t G_B17_0 = 0;
	{
		int32_t L_0 = __this->___Px;
		V_0 = L_0;
		V_1 = 0;
		goto IL_025e;
	}

IL_000e:
	{
		int32_t L_1 = __this->____row;
		int32_t L_2 = V_0;
		if ((((int32_t)L_1) >= ((int32_t)L_2)))
		{
			goto IL_0083;
		}
	}
	{
		int32_t L_3 = __this->____row;
		V_3 = L_3;
		V_4 = 0;
		goto IL_0079;
	}

IL_0023:
	{
		int32_t L_4 = V_3;
		int32_t L_5 = V_0;
		int32_t L_6 = V_4;
		V_5 = ((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(L_4, L_5)), L_6));
		C4U5BU5D_tA0B6DD53ECB5C3687C30C1AE88FBB09AA96AC22B* L_7 = __this->____col;
		int32_t L_8 = V_5;
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_9 = __this->____scene;
		Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_10 = __this->____cam;
		int32_t L_11 = V_4;
		int32_t L_12 = V_0;
		int32_t L_13 = V_3;
		int32_t L_14 = V_0;
		int32_t L_15 = __this->____backdrop;
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_16 = __this->____ids;
		int32_t L_17 = V_5;
		NullCheck(L_16);
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F L_18;
		L_18 = PayloadRender_Sample_mE7A4F8364A54709D1A58A600D4D029ED4B123C4A(L_9, L_10, ((float)(((float)il2cpp_codegen_add(((float)L_11), (0.5f)))/((float)L_12))), ((float)(((float)il2cpp_codegen_add(((float)L_13), (0.5f)))/((float)L_14))), L_15, ((L_16)->GetAddressAt(static_cast<il2cpp_array_size_t>(L_17))), NULL);
		NullCheck(L_7);
		(L_7)->SetAt(static_cast<il2cpp_array_size_t>(L_8), (C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F)L_18);
		int32_t L_19 = V_4;
		V_4 = ((int32_t)il2cpp_codegen_add(L_19, 1));
	}

IL_0079:
	{
		int32_t L_20 = V_4;
		int32_t L_21 = V_0;
		if ((((int32_t)L_20) < ((int32_t)L_21)))
		{
			goto IL_0023;
		}
	}
	{
		goto IL_024c;
	}

IL_0083:
	{
		int32_t L_22 = __this->____row;
		int32_t L_23 = V_0;
		V_2 = ((int32_t)il2cpp_codegen_subtract(L_22, L_23));
		V_6 = 0;
		goto IL_0244;
	}

IL_0094:
	{
		int32_t L_24 = V_2;
		int32_t L_25 = V_0;
		int32_t L_26 = V_6;
		V_7 = ((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(L_24, L_25)), L_26));
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_27 = __this->____ids;
		int32_t L_28 = V_7;
		NullCheck(L_27);
		int32_t L_29 = L_28;
		int32_t L_30 = (L_27)->GetAt(static_cast<il2cpp_array_size_t>(L_29));
		V_8 = L_30;
		int32_t L_31 = V_6;
		if ((((int32_t)L_31) <= ((int32_t)0)))
		{
			goto IL_00bb;
		}
	}
	{
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_32 = __this->____ids;
		int32_t L_33 = V_7;
		NullCheck(L_32);
		int32_t L_34 = ((int32_t)il2cpp_codegen_subtract(L_33, 1));
		int32_t L_35 = (L_32)->GetAt(static_cast<il2cpp_array_size_t>(L_34));
		int32_t L_36 = V_8;
		if ((!(((uint32_t)L_35) == ((uint32_t)L_36))))
		{
			goto IL_0101;
		}
	}

IL_00bb:
	{
		int32_t L_37 = V_6;
		int32_t L_38 = V_0;
		if ((((int32_t)L_37) >= ((int32_t)((int32_t)il2cpp_codegen_subtract(L_38, 1)))))
		{
			goto IL_00d1;
		}
	}
	{
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_39 = __this->____ids;
		int32_t L_40 = V_7;
		NullCheck(L_39);
		int32_t L_41 = ((int32_t)il2cpp_codegen_add(L_40, 1));
		int32_t L_42 = (L_39)->GetAt(static_cast<il2cpp_array_size_t>(L_41));
		int32_t L_43 = V_8;
		if ((!(((uint32_t)L_42) == ((uint32_t)L_43))))
		{
			goto IL_0101;
		}
	}

IL_00d1:
	{
		int32_t L_44 = V_2;
		if ((((int32_t)L_44) <= ((int32_t)0)))
		{
			goto IL_00e4;
		}
	}
	{
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_45 = __this->____ids;
		int32_t L_46 = V_7;
		int32_t L_47 = V_0;
		NullCheck(L_45);
		int32_t L_48 = ((int32_t)il2cpp_codegen_subtract(L_46, L_47));
		int32_t L_49 = (L_45)->GetAt(static_cast<il2cpp_array_size_t>(L_48));
		int32_t L_50 = V_8;
		if ((!(((uint32_t)L_49) == ((uint32_t)L_50))))
		{
			goto IL_0101;
		}
	}

IL_00e4:
	{
		int32_t L_51 = V_2;
		int32_t L_52 = V_0;
		if ((((int32_t)L_51) >= ((int32_t)((int32_t)il2cpp_codegen_subtract(L_52, 1)))))
		{
			goto IL_00fe;
		}
	}
	{
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_53 = __this->____ids;
		int32_t L_54 = V_7;
		int32_t L_55 = V_0;
		NullCheck(L_53);
		int32_t L_56 = ((int32_t)il2cpp_codegen_add(L_54, L_55));
		int32_t L_57 = (L_53)->GetAt(static_cast<il2cpp_array_size_t>(L_56));
		int32_t L_58 = V_8;
		G_B17_0 = ((((int32_t)((((int32_t)L_57) == ((int32_t)L_58))? 1 : 0)) == ((int32_t)0))? 1 : 0);
		goto IL_0102;
	}

IL_00fe:
	{
		G_B17_0 = 0;
		goto IL_0102;
	}

IL_0101:
	{
		G_B17_0 = 1;
	}

IL_0102:
	{
		C4U5BU5D_tA0B6DD53ECB5C3687C30C1AE88FBB09AA96AC22B* L_59 = __this->____col;
		int32_t L_60 = V_7;
		NullCheck(L_59);
		int32_t L_61 = L_60;
		C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F L_62 = (L_59)->GetAt(static_cast<il2cpp_array_size_t>(L_61));
		V_9 = L_62;
		if (!G_B17_0)
		{
			goto IL_01de;
		}
	}
	{
		il2cpp_codegen_initobj((&V_9), sizeof(C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F));
		V_11 = 0;
		goto IL_01d6;
	}

IL_0126:
	{
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_63 = __this->____scene;
		Cam_tEC8E349D27505A9C38386DDE6496344928DCC379 L_64 = __this->____cam;
		int32_t L_65 = V_6;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* L_66 = ((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___Rgss;
		int32_t L_67 = V_11;
		NullCheck(L_66);
		int32_t L_68 = ((int32_t)il2cpp_codegen_multiply(L_67, 2));
		float L_69 = (L_66)->GetAt(static_cast<il2cpp_array_size_t>(L_68));
		int32_t L_70 = V_0;
		int32_t L_71 = V_2;
		SingleU5BU5D_t89DEFE97BCEDB5857010E79ECE0F52CF6E93B87C* L_72 = ((PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_StaticFields*)il2cpp_codegen_static_fields_for(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var))->___Rgss;
		int32_t L_73 = V_11;
		NullCheck(L_72);
		int32_t L_74 = ((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(L_73, 2)), 1));
		float L_75 = (L_72)->GetAt(static_cast<il2cpp_array_size_t>(L_74));
		int32_t L_76 = V_0;
		int32_t L_77 = __this->____backdrop;
		C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F L_78;
		L_78 = PayloadRender_Sample_mE7A4F8364A54709D1A58A600D4D029ED4B123C4A(L_63, L_64, ((float)(((float)il2cpp_codegen_add(((float)il2cpp_codegen_add(((float)L_65), (0.5f))), L_69))/((float)L_70))), ((float)(((float)il2cpp_codegen_add(((float)il2cpp_codegen_add(((float)L_71), (0.5f))), L_75))/((float)L_76))), L_77, (&V_13), NULL);
		V_12 = L_78;
		float* L_79 = (float*)(&(&V_9)->___R);
		float* L_80 = L_79;
		float L_81 = *((float*)L_80);
		C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F L_82 = V_12;
		float L_83 = L_82.___R;
		*((float*)L_80) = (float)((float)il2cpp_codegen_add(L_81, ((float)il2cpp_codegen_multiply(L_83, (0.25f)))));
		float* L_84 = (float*)(&(&V_9)->___G);
		float* L_85 = L_84;
		float L_86 = *((float*)L_85);
		C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F L_87 = V_12;
		float L_88 = L_87.___G;
		*((float*)L_85) = (float)((float)il2cpp_codegen_add(L_86, ((float)il2cpp_codegen_multiply(L_88, (0.25f)))));
		float* L_89 = (float*)(&(&V_9)->___B);
		float* L_90 = L_89;
		float L_91 = *((float*)L_90);
		C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F L_92 = V_12;
		float L_93 = L_92.___B;
		*((float*)L_90) = (float)((float)il2cpp_codegen_add(L_91, ((float)il2cpp_codegen_multiply(L_93, (0.25f)))));
		float* L_94 = (float*)(&(&V_9)->___A);
		float* L_95 = L_94;
		float L_96 = *((float*)L_95);
		C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F L_97 = V_12;
		float L_98 = L_97.___A;
		*((float*)L_95) = (float)((float)il2cpp_codegen_add(L_96, ((float)il2cpp_codegen_multiply(L_98, (0.25f)))));
		int32_t L_99 = V_11;
		V_11 = ((int32_t)il2cpp_codegen_add(L_99, 1));
	}

IL_01d6:
	{
		int32_t L_100 = V_11;
		if ((((int32_t)L_100) < ((int32_t)4)))
		{
			goto IL_0126;
		}
	}

IL_01de:
	{
		int32_t L_101 = V_7;
		V_10 = ((int32_t)il2cpp_codegen_multiply(L_101, 4));
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_102 = __this->___Pixels;
		int32_t L_103 = V_10;
		C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F L_104 = V_9;
		float L_105 = L_104.___R;
		il2cpp_codegen_runtime_class_init_inline(PayloadRender_tFF67A7359AB67F191806722CCF19855ABD6FA3AC_il2cpp_TypeInfo_var);
		uint8_t L_106;
		L_106 = PayloadRender_Byte_m6D17F208B2A40D9E9DEDE0C4D5872C416E674974(L_105, NULL);
		NullCheck(L_102);
		(L_102)->SetAt(static_cast<il2cpp_array_size_t>(L_103), (uint8_t)L_106);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_107 = __this->___Pixels;
		int32_t L_108 = V_10;
		C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F L_109 = V_9;
		float L_110 = L_109.___G;
		uint8_t L_111;
		L_111 = PayloadRender_Byte_m6D17F208B2A40D9E9DEDE0C4D5872C416E674974(L_110, NULL);
		NullCheck(L_107);
		(L_107)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_108, 1))), (uint8_t)L_111);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_112 = __this->___Pixels;
		int32_t L_113 = V_10;
		C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F L_114 = V_9;
		float L_115 = L_114.___B;
		uint8_t L_116;
		L_116 = PayloadRender_Byte_m6D17F208B2A40D9E9DEDE0C4D5872C416E674974(L_115, NULL);
		NullCheck(L_112);
		(L_112)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_113, 2))), (uint8_t)L_116);
		ByteU5BU5D_tA6237BF417AE52AD70CFB4EF24A7A82613DF9031* L_117 = __this->___Pixels;
		int32_t L_118 = V_10;
		C4_tD10637C3FB636C9DCF850F82E748FEE40283AF9F L_119 = V_9;
		float L_120 = L_119.___A;
		uint8_t L_121;
		L_121 = PayloadRender_Byte_m6D17F208B2A40D9E9DEDE0C4D5872C416E674974(L_120, NULL);
		NullCheck(L_117);
		(L_117)->SetAt(static_cast<il2cpp_array_size_t>(((int32_t)il2cpp_codegen_add(L_118, 3))), (uint8_t)L_121);
		int32_t L_122 = V_6;
		V_6 = ((int32_t)il2cpp_codegen_add(L_122, 1));
	}

IL_0244:
	{
		int32_t L_123 = V_6;
		int32_t L_124 = V_0;
		if ((((int32_t)L_123) < ((int32_t)L_124)))
		{
			goto IL_0094;
		}
	}

IL_024c:
	{
		int32_t L_125 = V_1;
		V_1 = ((int32_t)il2cpp_codegen_add(L_125, 1));
		int32_t L_126 = __this->____row;
		__this->____row = ((int32_t)il2cpp_codegen_add(L_126, 1));
	}

IL_025e:
	{
		int32_t L_127 = V_1;
		int32_t L_128 = ___0_rows;
		if ((((int32_t)L_127) >= ((int32_t)L_128)))
		{
			goto IL_0270;
		}
	}
	{
		int32_t L_129 = __this->____row;
		int32_t L_130 = V_0;
		if ((((int32_t)L_129) < ((int32_t)((int32_t)il2cpp_codegen_multiply(L_130, 2)))))
		{
			goto IL_000e;
		}
	}

IL_0270:
	{
		bool L_131;
		L_131 = Job_get_Done_m16154739569DD8E47D867CA2FE93E82CCFAA9E22(__this, NULL);
		return L_131;
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
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D Prim_ToLocal_m3C6745FB67593049B03ED7B23000B1D2CE8618B2 (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_v, const RuntimeMethod* method) 
{
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = ___0_v;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_1 = __this->___U;
		float L_2;
		L_2 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_0, L_1, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_3 = ___0_v;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_4 = __this->___V;
		float L_5;
		L_5 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_3, L_4, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6 = ___0_v;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_7 = __this->___W;
		float L_8;
		L_8 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_6, L_7, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_9;
		memset((&L_9), 0, sizeof(L_9));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_9), L_2, L_5, L_8, NULL);
		return L_9;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Prim_Bake_mAC6BE30FFA8DAC0FDE930665B72453859C755AEB (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* __this, int32_t ___0_index, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	float V_1 = 0.0f;
	float V_2 = 0.0f;
	int32_t V_3 = 0;
	{
		int32_t L_0 = ___0_index;
		__this->___Seed = ((float)il2cpp_codegen_multiply(((float)L_0), (1.61800003f)));
		int32_t L_1 = __this->___Kind;
		V_3 = L_1;
		int32_t L_2 = V_3;
		switch (L_2)
		{
			case 0:
			{
				goto IL_0030;
			}
			case 1:
			{
				goto IL_005c;
			}
			case 2:
			{
				goto IL_0080;
			}
			case 3:
			{
				goto IL_00af;
			}
		}
	}
	{
		goto IL_00d3;
	}

IL_0030:
	{
		float L_3 = __this->___A;
		V_0 = L_3;
		float L_4 = __this->___B;
		V_1 = L_4;
		float L_5 = __this->___C;
		__this->___Z0 = ((-L_5));
		float L_6 = __this->___C;
		__this->___Z1 = L_6;
		goto IL_00f3;
	}

IL_005c:
	{
		float L_7 = __this->___A;
		float L_8 = L_7;
		V_1 = L_8;
		V_0 = L_8;
		float L_9 = __this->___C;
		__this->___Z0 = ((-L_9));
		float L_10 = __this->___C;
		__this->___Z1 = L_10;
		goto IL_00f3;
	}

IL_0080:
	{
		float L_11 = __this->___A;
		float L_12 = __this->___B;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_13;
		L_13 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA(L_11, L_12, NULL);
		float L_14 = L_13;
		V_1 = L_14;
		V_0 = L_14;
		float L_15 = __this->___C;
		__this->___Z0 = ((-L_15));
		float L_16 = __this->___C;
		__this->___Z1 = L_16;
		goto IL_00f3;
	}

IL_00af:
	{
		float L_17 = __this->___A;
		float L_18 = L_17;
		V_1 = L_18;
		V_0 = L_18;
		float L_19 = __this->___A;
		__this->___Z0 = ((-L_19));
		float L_20 = __this->___A;
		__this->___Z1 = L_20;
		goto IL_00f3;
	}

IL_00d3:
	{
		float L_21 = __this->___A;
		float L_22 = L_21;
		V_1 = L_22;
		V_0 = L_22;
		__this->___Z0 = (0.0f);
		float L_23;
		L_23 = Prim_get_DishDepth_m66DFD50533D955CCA37F193067B3DD66EB606B93(__this, NULL);
		__this->___Z1 = L_23;
	}

IL_00f3:
	{
		float L_24 = __this->___Z1;
		float L_25 = __this->___Z0;
		V_2 = ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_subtract(L_24, L_25)), (0.5f)));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_26 = __this->___O;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_27 = __this->___W;
		float L_28 = __this->___Z0;
		float L_29 = __this->___Z1;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_30;
		L_30 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_27, ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_add(L_28, L_29)), (0.5f))), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_31;
		L_31 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_26, L_30, NULL);
		__this->___Center = L_31;
		float L_32 = V_0;
		float L_33 = V_0;
		float L_34 = V_1;
		float L_35 = V_1;
		float L_36 = V_2;
		float L_37 = V_2;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_38;
		L_38 = sqrt(((double)((float)il2cpp_codegen_add(((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(L_32, L_33)), ((float)il2cpp_codegen_multiply(L_34, L_35)))), ((float)il2cpp_codegen_multiply(L_36, L_37))))));
		__this->___Radius = ((float)il2cpp_codegen_add(((float)L_38), (0.00100000005f)));
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float Prim_get_DishDepth_m66DFD50533D955CCA37F193067B3DD66EB606B93 (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		float L_0 = __this->___C;
		float L_1 = __this->___C;
		float L_2 = __this->___C;
		float L_3 = __this->___A;
		float L_4 = __this->___A;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_5;
		L_5 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA((0.0f), ((float)il2cpp_codegen_subtract(((float)il2cpp_codegen_multiply(L_1, L_2)), ((float)il2cpp_codegen_multiply(L_3, L_4)))), NULL);
		double L_6;
		L_6 = sqrt(((double)L_5));
		return ((float)il2cpp_codegen_subtract(L_0, ((float)L_6)));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* Prim_Corners_m512FD0FEA6F7C961CB55E658A3D7053C4700C542 (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* L_0 = (U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891*)il2cpp_codegen_object_new(U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891_il2cpp_TypeInfo_var);
		U3CCornersU3Ed__18__ctor_mE86F5A80976E4C0A18FB1F6BEB98314475C2B6F1(L_0, ((int32_t)-2), NULL);
		U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* L_1 = L_0;
		NullCheck(L_1);
		L_1->___U3CU3E4__this = __this;
		Il2CppCodeGenWriteBarrier((void**)(&L_1->___U3CU3E4__this), (void*)__this);
		return L_1;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Prim_Intersect_m6BE49C58D6429F03E78403320F517788BD7D3D84 (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_o, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_d, float ___2_maxT, float* ___3_t, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* ___4_n, const RuntimeMethod* method) 
{
	int32_t V_0 = 0;
	{
		int32_t L_0 = __this->___Kind;
		V_0 = L_0;
		int32_t L_1 = V_0;
		switch (L_1)
		{
			case 0:
			{
				goto IL_001f;
			}
			case 1:
			{
				goto IL_002d;
			}
			case 2:
			{
				goto IL_004c;
			}
			case 3:
			{
				goto IL_006b;
			}
		}
	}
	{
		goto IL_0079;
	}

IL_001f:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2 = ___0_o;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_3 = ___1_d;
		float L_4 = ___2_maxT;
		float* L_5 = ___3_t;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_6 = ___4_n;
		bool L_7;
		L_7 = Prim_HitBox_mEFE6D788F8CB72B3493221260D3214B3ED6A9CBE(__this, L_2, L_3, L_4, L_5, L_6, NULL);
		return L_7;
	}

IL_002d:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8 = ___0_o;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_9 = ___1_d;
		float L_10 = __this->___A;
		float L_11 = __this->___A;
		float L_12 = __this->___C;
		float L_13 = ___2_maxT;
		float* L_14 = ___3_t;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_15 = ___4_n;
		bool L_16;
		L_16 = Prim_HitCone_m422F74A22825028C177F99763BAB4BD83AD33CDF(L_8, L_9, L_10, L_11, L_12, L_13, L_14, L_15, NULL);
		return L_16;
	}

IL_004c:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_17 = ___0_o;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_18 = ___1_d;
		float L_19 = __this->___A;
		float L_20 = __this->___B;
		float L_21 = __this->___C;
		float L_22 = ___2_maxT;
		float* L_23 = ___3_t;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_24 = ___4_n;
		bool L_25;
		L_25 = Prim_HitCone_m422F74A22825028C177F99763BAB4BD83AD33CDF(L_17, L_18, L_19, L_20, L_21, L_22, L_23, L_24, NULL);
		return L_25;
	}

IL_006b:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_26 = ___0_o;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_27 = ___1_d;
		float L_28 = ___2_maxT;
		float* L_29 = ___3_t;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_30 = ___4_n;
		bool L_31;
		L_31 = Prim_HitSphere_m2AB861B39C6A2C12482F9EF6F60D177F7F1DC90F(__this, L_26, L_27, L_28, L_29, L_30, NULL);
		return L_31;
	}

IL_0079:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_32 = ___0_o;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_33 = ___1_d;
		float L_34 = ___2_maxT;
		float* L_35 = ___3_t;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_36 = ___4_n;
		bool L_37;
		L_37 = Prim_HitDish_m77A557A1FBE93CEE0CE49326D2E12F5EC9A80E95(__this, L_32, L_33, L_34, L_35, L_36, NULL);
		return L_37;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Prim_HitBox_mEFE6D788F8CB72B3493221260D3214B3ED6A9CBE (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_o, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_d, float ___2_maxT, float* ___3_t, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* ___4_n, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	float V_1 = 0.0f;
	int32_t V_2 = 0;
	float V_3 = 0.0f;
	int32_t V_4 = 0;
	float V_5 = 0.0f;
	float V_6 = 0.0f;
	float V_7 = 0.0f;
	float V_8 = 0.0f;
	float V_9 = 0.0f;
	float V_10 = 0.0f;
	float G_B6_0 = 0.0f;
	float G_B11_0 = 0.0f;
	float G_B16_0 = 0.0f;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* G_B37_0 = NULL;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* G_B34_0 = NULL;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* G_B36_0 = NULL;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* G_B35_0 = NULL;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D G_B38_0;
	memset((&G_B38_0), 0, sizeof(G_B38_0));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* G_B38_1 = NULL;
	{
		float* L_0 = ___3_t;
		*((float*)L_0) = (float)(0.0f);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_1 = ___4_n;
		il2cpp_codegen_initobj(L_1, sizeof(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D));
		V_0 = (-1.0E+09f);
		V_1 = (1.0E+09f);
		V_2 = 0;
		V_3 = (1.0f);
		V_4 = 0;
		goto IL_00ff;
	}

IL_002c:
	{
		int32_t L_2 = V_4;
		if (!L_2)
		{
			goto IL_0045;
		}
	}
	{
		int32_t L_3 = V_4;
		if ((((int32_t)L_3) == ((int32_t)1)))
		{
			goto IL_003d;
		}
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_4 = ___0_o;
		float L_5 = L_4.___Z;
		G_B6_0 = L_5;
		goto IL_004b;
	}

IL_003d:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6 = ___0_o;
		float L_7 = L_6.___Y;
		G_B6_0 = L_7;
		goto IL_004b;
	}

IL_0045:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8 = ___0_o;
		float L_9 = L_8.___X;
		G_B6_0 = L_9;
	}

IL_004b:
	{
		V_5 = G_B6_0;
		int32_t L_10 = V_4;
		if (!L_10)
		{
			goto IL_0066;
		}
	}
	{
		int32_t L_11 = V_4;
		if ((((int32_t)L_11) == ((int32_t)1)))
		{
			goto IL_005e;
		}
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12 = ___1_d;
		float L_13 = L_12.___Z;
		G_B11_0 = L_13;
		goto IL_006c;
	}

IL_005e:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_14 = ___1_d;
		float L_15 = L_14.___Y;
		G_B11_0 = L_15;
		goto IL_006c;
	}

IL_0066:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_16 = ___1_d;
		float L_17 = L_16.___X;
		G_B11_0 = L_17;
	}

IL_006c:
	{
		V_6 = G_B11_0;
		int32_t L_18 = V_4;
		if (!L_18)
		{
			goto IL_0087;
		}
	}
	{
		int32_t L_19 = V_4;
		if ((((int32_t)L_19) == ((int32_t)1)))
		{
			goto IL_007f;
		}
	}
	{
		float L_20 = __this->___C;
		G_B16_0 = L_20;
		goto IL_008d;
	}

IL_007f:
	{
		float L_21 = __this->___B;
		G_B16_0 = L_21;
		goto IL_008d;
	}

IL_0087:
	{
		float L_22 = __this->___A;
		G_B16_0 = L_22;
	}

IL_008d:
	{
		V_7 = G_B16_0;
		float L_23 = V_6;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_24;
		L_24 = fabsf(L_23);
		if ((!(((float)L_24) < ((float)(9.99999994E-09f)))))
		{
			goto IL_00ac;
		}
	}
	{
		float L_25 = V_5;
		float L_26 = V_7;
		if ((((float)L_25) < ((float)((-L_26)))))
		{
			goto IL_00aa;
		}
	}
	{
		float L_27 = V_5;
		float L_28 = V_7;
		if ((!(((float)L_27) > ((float)L_28))))
		{
			goto IL_00f9;
		}
	}

IL_00aa:
	{
		return (bool)0;
	}

IL_00ac:
	{
		float L_29 = V_7;
		float L_30 = V_5;
		float L_31 = V_6;
		V_8 = ((float)(((float)il2cpp_codegen_subtract(((-L_29)), L_30))/L_31));
		float L_32 = V_7;
		float L_33 = V_5;
		float L_34 = V_6;
		V_9 = ((float)(((float)il2cpp_codegen_subtract(L_32, L_33))/L_34));
		V_10 = (-1.0f);
		float L_35 = V_8;
		float L_36 = V_9;
		if ((!(((float)L_35) > ((float)L_36))))
		{
			goto IL_00dd;
		}
	}
	{
		float L_37 = V_8;
		float L_38 = V_9;
		V_8 = L_38;
		V_9 = L_37;
		V_10 = (1.0f);
	}

IL_00dd:
	{
		float L_39 = V_8;
		float L_40 = V_0;
		if ((!(((float)L_39) > ((float)L_40))))
		{
			goto IL_00eb;
		}
	}
	{
		float L_41 = V_8;
		V_0 = L_41;
		int32_t L_42 = V_4;
		V_2 = L_42;
		float L_43 = V_10;
		V_3 = L_43;
	}

IL_00eb:
	{
		float L_44 = V_9;
		float L_45 = V_1;
		if ((!(((float)L_44) < ((float)L_45))))
		{
			goto IL_00f3;
		}
	}
	{
		float L_46 = V_9;
		V_1 = L_46;
	}

IL_00f3:
	{
		float L_47 = V_0;
		float L_48 = V_1;
		if ((!(((float)L_47) > ((float)L_48))))
		{
			goto IL_00f9;
		}
	}
	{
		return (bool)0;
	}

IL_00f9:
	{
		int32_t L_49 = V_4;
		V_4 = ((int32_t)il2cpp_codegen_add(L_49, 1));
	}

IL_00ff:
	{
		int32_t L_50 = V_4;
		if ((((int32_t)L_50) < ((int32_t)3)))
		{
			goto IL_002c;
		}
	}
	{
		float L_51 = V_0;
		if ((((float)L_51) < ((float)(9.99999975E-05f))))
		{
			goto IL_0113;
		}
	}
	{
		float L_52 = V_0;
		float L_53 = ___2_maxT;
		if ((!(((float)L_52) >= ((float)L_53))))
		{
			goto IL_0115;
		}
	}

IL_0113:
	{
		return (bool)0;
	}

IL_0115:
	{
		float* L_54 = ___3_t;
		float L_55 = V_0;
		*((float*)L_54) = (float)L_55;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_56 = ___4_n;
		int32_t L_57 = V_2;
		if (!L_57)
		{
			G_B37_0 = L_56;
			goto IL_0146;
		}
		G_B34_0 = L_56;
	}
	{
		int32_t L_58 = V_2;
		if ((((int32_t)L_58) == ((int32_t)1)))
		{
			G_B36_0 = G_B34_0;
			goto IL_0134;
		}
		G_B35_0 = G_B34_0;
	}
	{
		float L_59 = V_3;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_60;
		memset((&L_60), 0, sizeof(L_60));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_60), (0.0f), (0.0f), L_59, NULL);
		G_B38_0 = L_60;
		G_B38_1 = G_B35_0;
		goto IL_0156;
	}

IL_0134:
	{
		float L_61 = V_3;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_62;
		memset((&L_62), 0, sizeof(L_62));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_62), (0.0f), L_61, (0.0f), NULL);
		G_B38_0 = L_62;
		G_B38_1 = G_B36_0;
		goto IL_0156;
	}

IL_0146:
	{
		float L_63 = V_3;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_64;
		memset((&L_64), 0, sizeof(L_64));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_64), L_63, (0.0f), (0.0f), NULL);
		G_B38_0 = L_64;
		G_B38_1 = G_B37_0;
	}

IL_0156:
	{
		*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)G_B38_1 = G_B38_0;
		return (bool)1;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Prim_HitCone_m422F74A22825028C177F99763BAB4BD83AD33CDF (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_o, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_d, float ___2_r0, float ___3_r1, float ___4_h, float ___5_maxT, float* ___6_t, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* ___7_n, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	bool V_0 = false;
	float V_1 = 0.0f;
	float V_2 = 0.0f;
	float V_3 = 0.0f;
	float V_4 = 0.0f;
	float V_5 = 0.0f;
	float V_6 = 0.0f;
	float V_7 = 0.0f;
	float V_8 = 0.0f;
	int32_t V_9 = 0;
	float V_10 = 0.0f;
	float V_11 = 0.0f;
	float V_12 = 0.0f;
	float V_13 = 0.0f;
	float V_14 = 0.0f;
	int32_t V_15 = 0;
	float V_16 = 0.0f;
	float V_17 = 0.0f;
	float V_18 = 0.0f;
	float G_B5_0 = 0.0f;
	float G_B4_0 = 0.0f;
	float G_B6_0 = 0.0f;
	float G_B6_1 = 0.0f;
	float G_B19_0 = 0.0f;
	float G_B21_0 = 0.0f;
	float G_B20_0 = 0.0f;
	float G_B22_0 = 0.0f;
	float G_B22_1 = 0.0f;
	float G_B27_0 = 0.0f;
	float G_B27_1 = 0.0f;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* G_B27_2 = NULL;
	float G_B26_0 = 0.0f;
	float G_B26_1 = 0.0f;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* G_B26_2 = NULL;
	float G_B28_0 = 0.0f;
	float G_B28_1 = 0.0f;
	float G_B28_2 = 0.0f;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* G_B28_3 = NULL;
	{
		float* L_0 = ___6_t;
		float L_1 = ___5_maxT;
		*((float*)L_0) = (float)L_1;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_2 = ___7_n;
		il2cpp_codegen_initobj(L_2, sizeof(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D));
		V_0 = (bool)0;
		float L_3 = ___3_r1;
		float L_4 = ___2_r0;
		float L_5 = ___4_h;
		V_1 = ((float)(((float)il2cpp_codegen_subtract(L_3, L_4))/((float)il2cpp_codegen_multiply((2.0f), L_5))));
		float L_6 = ___2_r0;
		float L_7 = ___3_r1;
		V_2 = ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_add(L_6, L_7)), (0.5f)));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8 = ___1_d;
		float L_9 = L_8.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10 = ___1_d;
		float L_11 = L_10.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12 = ___1_d;
		float L_13 = L_12.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_14 = ___1_d;
		float L_15 = L_14.___Y;
		float L_16 = V_1;
		float L_17 = V_1;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_18 = ___1_d;
		float L_19 = L_18.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_20 = ___1_d;
		float L_21 = L_20.___Z;
		V_3 = ((float)il2cpp_codegen_subtract(((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(L_9, L_11)), ((float)il2cpp_codegen_multiply(L_13, L_15)))), ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_multiply(L_16, L_17)), L_19)), L_21))));
		float L_22 = V_1;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_23 = ___0_o;
		float L_24 = L_23.___Z;
		float L_25 = V_2;
		V_4 = ((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(L_22, L_24)), L_25));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_26 = ___0_o;
		float L_27 = L_26.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_28 = ___1_d;
		float L_29 = L_28.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_30 = ___0_o;
		float L_31 = L_30.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_32 = ___1_d;
		float L_33 = L_32.___Y;
		float L_34 = V_1;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_35 = ___1_d;
		float L_36 = L_35.___Z;
		float L_37 = V_4;
		V_5 = ((float)il2cpp_codegen_multiply((2.0f), ((float)il2cpp_codegen_subtract(((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(L_27, L_29)), ((float)il2cpp_codegen_multiply(L_31, L_33)))), ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_multiply(L_34, L_36)), L_37))))));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_38 = ___0_o;
		float L_39 = L_38.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_40 = ___0_o;
		float L_41 = L_40.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_42 = ___0_o;
		float L_43 = L_42.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_44 = ___0_o;
		float L_45 = L_44.___Y;
		float L_46 = V_4;
		float L_47 = V_4;
		V_6 = ((float)il2cpp_codegen_subtract(((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(L_39, L_41)), ((float)il2cpp_codegen_multiply(L_43, L_45)))), ((float)il2cpp_codegen_multiply(L_46, L_47))));
		float L_48 = V_3;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_49;
		L_49 = fabsf(L_48);
		if ((!(((float)L_49) > ((float)(9.99999972E-10f)))))
		{
			goto IL_019f;
		}
	}
	{
		float L_50 = V_5;
		float L_51 = V_5;
		float L_52 = V_3;
		float L_53 = V_6;
		V_7 = ((float)il2cpp_codegen_subtract(((float)il2cpp_codegen_multiply(L_50, L_51)), ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_multiply((4.0f), L_52)), L_53))));
		float L_54 = V_7;
		if ((!(((float)L_54) >= ((float)(0.0f)))))
		{
			goto IL_019f;
		}
	}
	{
		float L_55 = V_7;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_56;
		L_56 = sqrt(((double)L_55));
		V_8 = ((float)L_56);
		V_9 = 0;
		goto IL_0197;
	}

IL_00f3:
	{
		float L_57 = V_5;
		int32_t L_58 = V_9;
		if (!L_58)
		{
			G_B5_0 = ((-L_57));
			goto IL_00fe;
		}
		G_B4_0 = ((-L_57));
	}
	{
		float L_59 = V_8;
		G_B6_0 = L_59;
		G_B6_1 = G_B4_0;
		goto IL_0101;
	}

IL_00fe:
	{
		float L_60 = V_8;
		G_B6_0 = ((-L_60));
		G_B6_1 = G_B5_0;
	}

IL_0101:
	{
		float L_61 = V_3;
		V_10 = ((float)(((float)il2cpp_codegen_add(G_B6_1, G_B6_0))/((float)il2cpp_codegen_multiply((2.0f), L_61))));
		float L_62 = V_10;
		if ((((float)L_62) < ((float)(9.99999975E-05f))))
		{
			goto IL_0191;
		}
	}
	{
		float L_63 = V_10;
		float* L_64 = ___6_t;
		float L_65 = *((float*)L_64);
		if ((((float)L_63) >= ((float)L_65)))
		{
			goto IL_0191;
		}
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_66 = ___0_o;
		float L_67 = L_66.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_68 = ___1_d;
		float L_69 = L_68.___Z;
		float L_70 = V_10;
		V_11 = ((float)il2cpp_codegen_add(L_67, ((float)il2cpp_codegen_multiply(L_69, L_70))));
		float L_71 = V_11;
		float L_72 = ___4_h;
		if ((((float)L_71) < ((float)((-L_72)))))
		{
			goto IL_0191;
		}
	}
	{
		float L_73 = V_11;
		float L_74 = ___4_h;
		if ((((float)L_73) > ((float)L_74)))
		{
			goto IL_0191;
		}
	}
	{
		float L_75 = V_1;
		float L_76 = V_11;
		float L_77 = V_2;
		V_12 = ((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(L_75, L_76)), L_77));
		float L_78 = V_12;
		if ((((float)L_78) < ((float)(0.0f))))
		{
			goto IL_0191;
		}
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_79 = ___0_o;
		float L_80 = L_79.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_81 = ___1_d;
		float L_82 = L_81.___X;
		float L_83 = V_10;
		V_13 = ((float)il2cpp_codegen_add(L_80, ((float)il2cpp_codegen_multiply(L_82, L_83))));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_84 = ___0_o;
		float L_85 = L_84.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_86 = ___1_d;
		float L_87 = L_86.___Y;
		float L_88 = V_10;
		V_14 = ((float)il2cpp_codegen_add(L_85, ((float)il2cpp_codegen_multiply(L_87, L_88))));
		float* L_89 = ___6_t;
		float L_90 = V_10;
		*((float*)L_89) = (float)L_90;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_91 = ___7_n;
		float L_92 = V_13;
		float L_93 = V_14;
		float L_94 = V_1;
		float L_95 = V_12;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_96;
		memset((&L_96), 0, sizeof(L_96));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_96), L_92, L_93, ((float)il2cpp_codegen_multiply(((-L_94)), L_95)), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_97;
		L_97 = V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892(L_96, NULL);
		*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_91 = L_97;
		V_0 = (bool)1;
	}

IL_0191:
	{
		int32_t L_98 = V_9;
		V_9 = ((int32_t)il2cpp_codegen_add(L_98, 1));
	}

IL_0197:
	{
		int32_t L_99 = V_9;
		if ((((int32_t)L_99) < ((int32_t)2)))
		{
			goto IL_00f3;
		}
	}

IL_019f:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_100 = ___1_d;
		float L_101 = L_100.___Z;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_102;
		L_102 = fabsf(L_101);
		if ((!(((float)L_102) > ((float)(9.99999972E-10f)))))
		{
			goto IL_025d;
		}
	}
	{
		V_15 = 0;
		goto IL_0255;
	}

IL_01bc:
	{
		int32_t L_103 = V_15;
		if (!L_103)
		{
			goto IL_01c4;
		}
	}
	{
		float L_104 = ___4_h;
		G_B19_0 = L_104;
		goto IL_01c7;
	}

IL_01c4:
	{
		float L_105 = ___4_h;
		G_B19_0 = ((-L_105));
	}

IL_01c7:
	{
		int32_t L_106 = V_15;
		if (!L_106)
		{
			G_B21_0 = G_B19_0;
			goto IL_01ce;
		}
		G_B20_0 = G_B19_0;
	}
	{
		float L_107 = ___3_r1;
		G_B22_0 = L_107;
		G_B22_1 = G_B20_0;
		goto IL_01cf;
	}

IL_01ce:
	{
		float L_108 = ___2_r0;
		G_B22_0 = L_108;
		G_B22_1 = G_B21_0;
	}

IL_01cf:
	{
		V_16 = G_B22_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_109 = ___0_o;
		float L_110 = L_109.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_111 = ___1_d;
		float L_112 = L_111.___Z;
		V_17 = ((float)(((float)il2cpp_codegen_subtract(G_B22_1, L_110))/L_112));
		float L_113 = V_17;
		if ((((float)L_113) < ((float)(9.99999975E-05f))))
		{
			goto IL_024f;
		}
	}
	{
		float L_114 = V_17;
		float* L_115 = ___6_t;
		float L_116 = *((float*)L_115);
		if ((((float)L_114) >= ((float)L_116)))
		{
			goto IL_024f;
		}
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_117 = ___0_o;
		float L_118 = L_117.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_119 = ___1_d;
		float L_120 = L_119.___X;
		float L_121 = V_17;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_122 = ___0_o;
		float L_123 = L_122.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_124 = ___1_d;
		float L_125 = L_124.___Y;
		float L_126 = V_17;
		V_18 = ((float)il2cpp_codegen_add(L_123, ((float)il2cpp_codegen_multiply(L_125, L_126))));
		float L_127 = ((float)il2cpp_codegen_add(L_118, ((float)il2cpp_codegen_multiply(L_120, L_121))));
		float L_128 = V_18;
		float L_129 = V_18;
		float L_130 = V_16;
		float L_131 = V_16;
		if ((((float)((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(L_127, L_127)), ((float)il2cpp_codegen_multiply(L_128, L_129))))) > ((float)((float)il2cpp_codegen_multiply(L_130, L_131)))))
		{
			goto IL_024f;
		}
	}
	{
		float* L_132 = ___6_t;
		float L_133 = V_17;
		*((float*)L_132) = (float)L_133;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_134 = ___7_n;
		int32_t L_135 = V_15;
		if (!L_135)
		{
			G_B27_0 = (0.0f);
			G_B27_1 = (0.0f);
			G_B27_2 = L_134;
			goto IL_023e;
		}
		G_B26_0 = (0.0f);
		G_B26_1 = (0.0f);
		G_B26_2 = L_134;
	}
	{
		G_B28_0 = (1.0f);
		G_B28_1 = G_B26_0;
		G_B28_2 = G_B26_1;
		G_B28_3 = G_B26_2;
		goto IL_0243;
	}

IL_023e:
	{
		G_B28_0 = (-1.0f);
		G_B28_1 = G_B27_0;
		G_B28_2 = G_B27_1;
		G_B28_3 = G_B27_2;
	}

IL_0243:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_136;
		memset((&L_136), 0, sizeof(L_136));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_136), G_B28_2, G_B28_1, G_B28_0, NULL);
		*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)G_B28_3 = L_136;
		V_0 = (bool)1;
	}

IL_024f:
	{
		int32_t L_137 = V_15;
		V_15 = ((int32_t)il2cpp_codegen_add(L_137, 1));
	}

IL_0255:
	{
		int32_t L_138 = V_15;
		if ((((int32_t)L_138) < ((int32_t)2)))
		{
			goto IL_01bc;
		}
	}

IL_025d:
	{
		bool L_139 = V_0;
		return L_139;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Prim_HitSphere_m2AB861B39C6A2C12482F9EF6F60D177F7F1DC90F (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_o, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_d, float ___2_maxT, float* ___3_t, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* ___4_n, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	float V_1 = 0.0f;
	float V_2 = 0.0f;
	float V_3 = 0.0f;
	float V_4 = 0.0f;
	{
		float* L_0 = ___3_t;
		*((float*)L_0) = (float)(0.0f);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_1 = ___4_n;
		il2cpp_codegen_initobj(L_1, sizeof(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2 = ___0_o;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_3 = ___1_d;
		float L_4;
		L_4 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_2, L_3, NULL);
		V_0 = L_4;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_5 = ___0_o;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6 = ___0_o;
		float L_7;
		L_7 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_5, L_6, NULL);
		float L_8 = __this->___A;
		float L_9 = __this->___A;
		V_1 = ((float)il2cpp_codegen_subtract(L_7, ((float)il2cpp_codegen_multiply(L_8, L_9))));
		float L_10 = V_0;
		float L_11 = V_0;
		float L_12 = V_1;
		V_2 = ((float)il2cpp_codegen_subtract(((float)il2cpp_codegen_multiply(L_10, L_11)), L_12));
		float L_13 = V_2;
		if ((!(((float)L_13) < ((float)(0.0f)))))
		{
			goto IL_003e;
		}
	}
	{
		return (bool)0;
	}

IL_003e:
	{
		float L_14 = V_2;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_15;
		L_15 = sqrt(((double)L_14));
		V_3 = ((float)L_15);
		float L_16 = V_0;
		float L_17 = V_3;
		V_4 = ((float)il2cpp_codegen_subtract(((-L_16)), L_17));
		float L_18 = V_4;
		if ((!(((float)L_18) < ((float)(9.99999975E-05f)))))
		{
			goto IL_005c;
		}
	}
	{
		float L_19 = V_0;
		float L_20 = V_3;
		V_4 = ((float)il2cpp_codegen_add(((-L_19)), L_20));
	}

IL_005c:
	{
		float L_21 = V_4;
		if ((((float)L_21) < ((float)(9.99999975E-05f))))
		{
			goto IL_006a;
		}
	}
	{
		float L_22 = V_4;
		float L_23 = ___2_maxT;
		if ((!(((float)L_22) >= ((float)L_23))))
		{
			goto IL_006c;
		}
	}

IL_006a:
	{
		return (bool)0;
	}

IL_006c:
	{
		float* L_24 = ___3_t;
		float L_25 = V_4;
		*((float*)L_24) = (float)L_25;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_26 = ___4_n;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_27 = ___0_o;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_28 = ___1_d;
		float L_29 = V_4;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_30;
		L_30 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_28, L_29, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_31;
		L_31 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_27, L_30, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_32;
		L_32 = V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892(L_31, NULL);
		*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_26 = L_32;
		return (bool)1;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Prim_HitDish_m77A557A1FBE93CEE0CE49326D2E12F5EC9A80E95 (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_o, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_d, float ___2_maxT, float* ___3_t, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* ___4_n, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	float V_1 = 0.0f;
	float V_2 = 0.0f;
	float V_3 = 0.0f;
	float V_4 = 0.0f;
	bool V_5 = false;
	int32_t V_6 = 0;
	float V_7 = 0.0f;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_8;
	memset((&V_8), 0, sizeof(V_8));
	float G_B6_0 = 0.0f;
	{
		float* L_0 = ___3_t;
		float L_1 = ___2_maxT;
		*((float*)L_0) = (float)L_1;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_2 = ___4_n;
		il2cpp_codegen_initobj(L_2, sizeof(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_3 = ___0_o;
		float L_4 = __this->___C;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_5;
		memset((&L_5), 0, sizeof(L_5));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_5), (0.0f), (0.0f), L_4, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6;
		L_6 = V3_op_Subtraction_mBF754B8B036EFB8C8B32E2C6A9799AFC1B245482(L_3, L_5, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_7 = L_6;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8 = ___1_d;
		float L_9;
		L_9 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_7, L_8, NULL);
		V_0 = L_9;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10 = L_7;
		float L_11;
		L_11 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_10, L_10, NULL);
		float L_12 = __this->___C;
		float L_13 = __this->___C;
		V_1 = ((float)il2cpp_codegen_subtract(L_11, ((float)il2cpp_codegen_multiply(L_12, L_13))));
		float L_14 = V_0;
		float L_15 = V_0;
		float L_16 = V_1;
		V_2 = ((float)il2cpp_codegen_subtract(((float)il2cpp_codegen_multiply(L_14, L_15)), L_16));
		float L_17 = V_2;
		if ((!(((float)L_17) < ((float)(0.0f)))))
		{
			goto IL_0054;
		}
	}
	{
		return (bool)0;
	}

IL_0054:
	{
		float L_18 = V_2;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_19;
		L_19 = sqrt(((double)L_18));
		V_3 = ((float)L_19);
		float L_20;
		L_20 = Prim_get_DishDepth_m66DFD50533D955CCA37F193067B3DD66EB606B93(__this, NULL);
		V_4 = L_20;
		V_5 = (bool)0;
		V_6 = 0;
		goto IL_00f1;
	}

IL_0070:
	{
		int32_t L_21 = V_6;
		if (!L_21)
		{
			goto IL_007a;
		}
	}
	{
		float L_22 = V_0;
		float L_23 = V_3;
		G_B6_0 = ((float)il2cpp_codegen_add(((-L_22)), L_23));
		goto IL_007e;
	}

IL_007a:
	{
		float L_24 = V_0;
		float L_25 = V_3;
		G_B6_0 = ((float)il2cpp_codegen_subtract(((-L_24)), L_25));
	}

IL_007e:
	{
		V_7 = G_B6_0;
		float L_26 = V_7;
		if ((((float)L_26) < ((float)(9.99999975E-05f))))
		{
			goto IL_00eb;
		}
	}
	{
		float L_27 = V_7;
		float* L_28 = ___3_t;
		float L_29 = *((float*)L_28);
		if ((((float)L_27) >= ((float)L_29)))
		{
			goto IL_00eb;
		}
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_30 = ___0_o;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_31 = ___1_d;
		float L_32 = V_7;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_33;
		L_33 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_31, L_32, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_34;
		L_34 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_30, L_33, NULL);
		V_8 = L_34;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_35 = V_8;
		float L_36 = L_35.___Z;
		float L_37 = V_4;
		if ((((float)L_36) > ((float)L_37)))
		{
			goto IL_00eb;
		}
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_38 = V_8;
		float L_39 = L_38.___Z;
		if ((((float)L_39) < ((float)(0.0f))))
		{
			goto IL_00eb;
		}
	}
	{
		float* L_40 = ___3_t;
		float L_41 = V_7;
		*((float*)L_40) = (float)L_41;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_42 = ___4_n;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_43 = V_8;
		float L_44 = __this->___C;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_45;
		memset((&L_45), 0, sizeof(L_45));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_45), (0.0f), (0.0f), L_44, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_46;
		L_46 = V3_op_Subtraction_mBF754B8B036EFB8C8B32E2C6A9799AFC1B245482(L_43, L_45, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_47;
		L_47 = V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892(L_46, NULL);
		*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_42 = L_47;
		V_5 = (bool)1;
		goto IL_00f9;
	}

IL_00eb:
	{
		int32_t L_48 = V_6;
		V_6 = ((int32_t)il2cpp_codegen_add(L_48, 1));
	}

IL_00f1:
	{
		int32_t L_49 = V_6;
		if ((((int32_t)L_49) < ((int32_t)2)))
		{
			goto IL_0070;
		}
	}

IL_00f9:
	{
		bool L_50 = V_5;
		return L_50;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Prim__ctor_m4C75AA3E29F3B4340FC7142CFBA005FDAA8EA60B (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* __this, const RuntimeMethod* method) 
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
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void U3CCornersU3Ed__18__ctor_mE86F5A80976E4C0A18FB1F6BEB98314475C2B6F1 (U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* __this, int32_t ___0_U3CU3E1__state, const RuntimeMethod* method) 
{
	{
		Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2(__this, NULL);
		int32_t L_0 = ___0_U3CU3E1__state;
		__this->___U3CU3E1__state = L_0;
		int32_t L_1;
		L_1 = Environment_get_CurrentManagedThreadId_m66483AADCCC13272EBDCD94D31D2E52603C24BDF(NULL);
		__this->___U3CU3El__initialThreadId = L_1;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void U3CCornersU3Ed__18_System_IDisposable_Dispose_m695CDB250BD643B47295E005878AF8601AFCCD7E (U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* __this, const RuntimeMethod* method) 
{
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool U3CCornersU3Ed__18_MoveNext_m4AB07C68678FD503B9360BC12B19C0660F7A8AF2 (U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* V_1 = NULL;
	float V_2 = 0.0f;
	float V_3 = 0.0f;
	float V_4 = 0.0f;
	int32_t V_5 = 0;
	U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* G_B7_0 = NULL;
	U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* G_B4_0 = NULL;
	U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* G_B6_0 = NULL;
	U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* G_B5_0 = NULL;
	float G_B8_0 = 0.0f;
	U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* G_B8_1 = NULL;
	U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* G_B10_0 = NULL;
	U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* G_B9_0 = NULL;
	float G_B11_0 = 0.0f;
	U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* G_B11_1 = NULL;
	float G_B15_0 = 0.0f;
	float G_B18_0 = 0.0f;
	float G_B21_0 = 0.0f;
	{
		int32_t L_0 = __this->___U3CU3E1__state;
		V_0 = L_0;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_1 = __this->___U3CU3E4__this;
		V_1 = L_1;
		int32_t L_2 = V_0;
		if (!L_2)
		{
			goto IL_001a;
		}
	}
	{
		int32_t L_3 = V_0;
		if ((((int32_t)L_3) == ((int32_t)1)))
		{
			goto IL_0118;
		}
	}
	{
		return (bool)0;
	}

IL_001a:
	{
		__this->___U3CU3E1__state = (-1);
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_4 = V_1;
		NullCheck(L_4);
		int32_t L_5 = L_4->___Kind;
		if (!L_5)
		{
			G_B7_0 = __this;
			goto IL_004e;
		}
		G_B4_0 = __this;
	}
	{
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_6 = V_1;
		NullCheck(L_6);
		int32_t L_7 = L_6->___Kind;
		if ((((int32_t)L_7) == ((int32_t)2)))
		{
			G_B6_0 = G_B4_0;
			goto IL_003b;
		}
		G_B5_0 = G_B4_0;
	}
	{
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_8 = V_1;
		NullCheck(L_8);
		float L_9 = L_8->___A;
		G_B8_0 = L_9;
		G_B8_1 = G_B5_0;
		goto IL_0054;
	}

IL_003b:
	{
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_10 = V_1;
		NullCheck(L_10);
		float L_11 = L_10->___A;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_12 = V_1;
		NullCheck(L_12);
		float L_13 = L_12->___B;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_14;
		L_14 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA(L_11, L_13, NULL);
		G_B8_0 = L_14;
		G_B8_1 = G_B6_0;
		goto IL_0054;
	}

IL_004e:
	{
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_15 = V_1;
		NullCheck(L_15);
		float L_16 = L_15->___A;
		G_B8_0 = L_16;
		G_B8_1 = G_B7_0;
	}

IL_0054:
	{
		NullCheck(G_B8_1);
		G_B8_1->___U3CexU3E5__2 = G_B8_0;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_17 = V_1;
		NullCheck(L_17);
		int32_t L_18 = L_17->___Kind;
		if (!L_18)
		{
			G_B10_0 = __this;
			goto IL_006a;
		}
		G_B9_0 = __this;
	}
	{
		float L_19 = __this->___U3CexU3E5__2;
		G_B11_0 = L_19;
		G_B11_1 = G_B9_0;
		goto IL_0070;
	}

IL_006a:
	{
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_20 = V_1;
		NullCheck(L_20);
		float L_21 = L_20->___B;
		G_B11_0 = L_21;
		G_B11_1 = G_B10_0;
	}

IL_0070:
	{
		NullCheck(G_B11_1);
		G_B11_1->___U3CeyU3E5__3 = G_B11_0;
		__this->___U3CiU3E5__4 = 0;
		goto IL_0131;
	}

IL_0081:
	{
		int32_t L_22 = __this->___U3CiU3E5__4;
		if (!((int32_t)(L_22&1)))
		{
			goto IL_0093;
		}
	}
	{
		float L_23 = __this->___U3CexU3E5__2;
		G_B15_0 = L_23;
		goto IL_009a;
	}

IL_0093:
	{
		float L_24 = __this->___U3CexU3E5__2;
		G_B15_0 = ((-L_24));
	}

IL_009a:
	{
		V_2 = G_B15_0;
		int32_t L_25 = __this->___U3CiU3E5__4;
		if (!((int32_t)(L_25&2)))
		{
			goto IL_00ad;
		}
	}
	{
		float L_26 = __this->___U3CeyU3E5__3;
		G_B18_0 = L_26;
		goto IL_00b4;
	}

IL_00ad:
	{
		float L_27 = __this->___U3CeyU3E5__3;
		G_B18_0 = ((-L_27));
	}

IL_00b4:
	{
		V_3 = G_B18_0;
		int32_t L_28 = __this->___U3CiU3E5__4;
		if (!((int32_t)(L_28&4)))
		{
			goto IL_00c7;
		}
	}
	{
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_29 = V_1;
		NullCheck(L_29);
		float L_30 = L_29->___Z1;
		G_B21_0 = L_30;
		goto IL_00cd;
	}

IL_00c7:
	{
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_31 = V_1;
		NullCheck(L_31);
		float L_32 = L_31->___Z0;
		G_B21_0 = L_32;
	}

IL_00cd:
	{
		V_4 = G_B21_0;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_33 = V_1;
		NullCheck(L_33);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_34 = L_33->___O;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_35 = V_1;
		NullCheck(L_35);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_36 = L_35->___U;
		float L_37 = V_2;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_38;
		L_38 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_36, L_37, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_39;
		L_39 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_34, L_38, NULL);
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_40 = V_1;
		NullCheck(L_40);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_41 = L_40->___V;
		float L_42 = V_3;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_43;
		L_43 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_41, L_42, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_44;
		L_44 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_39, L_43, NULL);
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_45 = V_1;
		NullCheck(L_45);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_46 = L_45->___W;
		float L_47 = V_4;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_48;
		L_48 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_46, L_47, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_49;
		L_49 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_44, L_48, NULL);
		__this->___U3CU3E2__current = L_49;
		__this->___U3CU3E1__state = 1;
		return (bool)1;
	}

IL_0118:
	{
		__this->___U3CU3E1__state = (-1);
		int32_t L_50 = __this->___U3CiU3E5__4;
		V_5 = L_50;
		int32_t L_51 = V_5;
		__this->___U3CiU3E5__4 = ((int32_t)il2cpp_codegen_add(L_51, 1));
	}

IL_0131:
	{
		int32_t L_52 = __this->___U3CiU3E5__4;
		if ((((int32_t)L_52) < ((int32_t)8)))
		{
			goto IL_0081;
		}
	}
	{
		return (bool)0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D U3CCornersU3Ed__18_System_Collections_Generic_IEnumeratorU3CKestrel_Core_Flight_PayloadRender_V3U3E_get_Current_mB84817BDA8E076860272A36B1172DD7F00E612EF (U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* __this, const RuntimeMethod* method) 
{
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = __this->___U3CU3E2__current;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void U3CCornersU3Ed__18_System_Collections_IEnumerator_Reset_m8965145E01DE54472727F2AAFC07B18E8B9E5CD5 (U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* __this, const RuntimeMethod* method) 
{
	{
		NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A* L_0 = (NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A*)il2cpp_codegen_object_new(((RuntimeClass*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A_il2cpp_TypeInfo_var)));
		NotSupportedException__ctor_m1398D0CDE19B36AA3DE9392879738C1EA2439CDF(L_0, NULL);
		IL2CPP_RAISE_MANAGED_EXCEPTION(L_0, ((RuntimeMethod*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&U3CCornersU3Ed__18_System_Collections_IEnumerator_Reset_m8965145E01DE54472727F2AAFC07B18E8B9E5CD5_RuntimeMethod_var)));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* U3CCornersU3Ed__18_System_Collections_IEnumerator_get_Current_m08D3DA454DDB3C68225501171F3FDCEB860C7DE6 (U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = __this->___U3CU3E2__current;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_1 = L_0;
		RuntimeObject* L_2 = Box(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D_il2cpp_TypeInfo_var, &L_1);
		return L_2;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* U3CCornersU3Ed__18_System_Collections_Generic_IEnumerableU3CKestrel_Core_Flight_PayloadRender_V3U3E_GetEnumerator_m5E6A1A1C95E40426B75378166F60E7E29D570EA4 (U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* V_0 = NULL;
	{
		int32_t L_0 = __this->___U3CU3E1__state;
		if ((!(((uint32_t)L_0) == ((uint32_t)((int32_t)-2)))))
		{
			goto IL_0022;
		}
	}
	{
		int32_t L_1 = __this->___U3CU3El__initialThreadId;
		int32_t L_2;
		L_2 = Environment_get_CurrentManagedThreadId_m66483AADCCC13272EBDCD94D31D2E52603C24BDF(NULL);
		if ((!(((uint32_t)L_1) == ((uint32_t)L_2))))
		{
			goto IL_0022;
		}
	}
	{
		__this->___U3CU3E1__state = 0;
		V_0 = __this;
		goto IL_0035;
	}

IL_0022:
	{
		U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* L_3 = (U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891*)il2cpp_codegen_object_new(U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891_il2cpp_TypeInfo_var);
		U3CCornersU3Ed__18__ctor_mE86F5A80976E4C0A18FB1F6BEB98314475C2B6F1(L_3, 0, NULL);
		V_0 = L_3;
		U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* L_4 = V_0;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_5 = __this->___U3CU3E4__this;
		NullCheck(L_4);
		L_4->___U3CU3E4__this = L_5;
		Il2CppCodeGenWriteBarrier((void**)(&L_4->___U3CU3E4__this), (void*)L_5);
	}

IL_0035:
	{
		U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* L_6 = V_0;
		return L_6;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* U3CCornersU3Ed__18_System_Collections_IEnumerable_GetEnumerator_m208B05A5C4A7AB8D68B1EFBE48D7A341D2DA1400 (U3CCornersU3Ed__18_tC859D61435BD4FF0699703EE22B64694CEA8E891* __this, const RuntimeMethod* method) 
{
	{
		RuntimeObject* L_0;
		L_0 = U3CCornersU3Ed__18_System_Collections_Generic_IEnumerableU3CKestrel_Core_Flight_PayloadRender_V3U3E_GetEnumerator_m5E6A1A1C95E40426B75378166F60E7E29D570EA4(__this, NULL);
		return L_0;
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
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder_Add_mCED8C193B63F017AC5E35654B49F1A6F1E06A8CF (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* __this, Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* ___0_p, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_Add_mE2377BD05FECB1E7EA1473B773D581201C95A728_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_get_Count_mA4B6A7483D74113D7978E2DA5D1DEE4F906ECC23_RuntimeMethod_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_0 = ___0_p;
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_1 = __this->___Parts;
		NullCheck(L_1);
		int32_t L_2;
		L_2 = List_1_get_Count_mA4B6A7483D74113D7978E2DA5D1DEE4F906ECC23_inline(L_1, List_1_get_Count_mA4B6A7483D74113D7978E2DA5D1DEE4F906ECC23_RuntimeMethod_var);
		NullCheck(L_0);
		Prim_Bake_mAC6BE30FFA8DAC0FDE930665B72453859C755AEB(L_0, L_2, NULL);
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_3 = __this->___Parts;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_4 = ___0_p;
		NullCheck(L_3);
		List_1_Add_mE2377BD05FECB1E7EA1473B773D581201C95A728_inline(L_3, L_4, List_1_Add_mE2377BD05FECB1E7EA1473B773D581201C95A728_RuntimeMethod_var);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder_Box_m86FA800943F0C86BE146FBD1F3910B77E894C9C5 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* __this, float ___0_x, float ___1_y, float ___2_z, float ___3_w, float ___4_h, float ___5_d, int32_t ___6_s, float ___7_yawDeg, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_1;
	memset((&V_1), 0, sizeof(V_1));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_2;
	memset((&V_2), 0, sizeof(V_2));
	{
		float L_0 = ___7_yawDeg;
		V_0 = ((float)(((float)il2cpp_codegen_multiply(L_0, (3.14159274f)))/(180.0f)));
		float L_1 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_2;
		L_2 = cos(((double)L_1));
		float L_3 = V_0;
		double L_4;
		L_4 = sin(((double)L_3));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&V_1), ((float)L_2), (0.0f), ((-((float)L_4))), NULL);
		float L_5 = V_0;
		double L_6;
		L_6 = sin(((double)L_5));
		float L_7 = V_0;
		double L_8;
		L_8 = cos(((double)L_7));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&V_2), ((float)L_6), (0.0f), ((float)L_8), NULL);
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_9 = (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6*)il2cpp_codegen_object_new(Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6_il2cpp_TypeInfo_var);
		Prim__ctor_m4C75AA3E29F3B4340FC7142CFBA005FDAA8EA60B(L_9, NULL);
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_10 = L_9;
		NullCheck(L_10);
		L_10->___Kind = 0;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_11 = L_10;
		float L_12 = ___0_x;
		float L_13 = ___1_y;
		float L_14 = ___2_z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_15;
		memset((&L_15), 0, sizeof(L_15));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_15), L_12, L_13, L_14, NULL);
		NullCheck(L_11);
		L_11->___O = L_15;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_16 = L_11;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_17 = V_1;
		NullCheck(L_16);
		L_16->___U = L_17;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_18 = L_16;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_19;
		memset((&L_19), 0, sizeof(L_19));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_19), (0.0f), (1.0f), (0.0f), NULL);
		NullCheck(L_18);
		L_18->___V = L_19;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_20 = L_18;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_21 = V_2;
		NullCheck(L_20);
		L_20->___W = L_21;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_22 = L_20;
		float L_23 = ___3_w;
		NullCheck(L_22);
		L_22->___A = ((float)il2cpp_codegen_multiply(L_23, (0.5f)));
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_24 = L_22;
		float L_25 = ___4_h;
		NullCheck(L_24);
		L_24->___B = ((float)il2cpp_codegen_multiply(L_25, (0.5f)));
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_26 = L_24;
		float L_27 = ___5_d;
		NullCheck(L_26);
		L_26->___C = ((float)il2cpp_codegen_multiply(L_27, (0.5f)));
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_28 = L_26;
		int32_t L_29 = ___6_s;
		NullCheck(L_28);
		L_28->___Surface = L_29;
		Builder_Add_mCED8C193B63F017AC5E35654B49F1A6F1E06A8CF(__this, L_28, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder_Slab_mAA7D3ABE211E7F908653A28B993A9888D6E4C9BE (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_c, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_axis, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___2_upHint, float ___3_w, float ___4_h, float ___5_d, int32_t ___6_s, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_0;
	memset((&V_0), 0, sizeof(V_0));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_1;
	memset((&V_1), 0, sizeof(V_1));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_2;
	memset((&V_2), 0, sizeof(V_2));
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = ___1_axis;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_1;
		L_1 = V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892(L_0, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2 = ___2_upHint;
		Builder_Basis_mCC3825AC627F97D83F0DF37825C687E62556B6EF(L_1, L_2, (&V_0), (&V_1), (&V_2), NULL);
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_3 = (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6*)il2cpp_codegen_object_new(Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6_il2cpp_TypeInfo_var);
		Prim__ctor_m4C75AA3E29F3B4340FC7142CFBA005FDAA8EA60B(L_3, NULL);
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_4 = L_3;
		NullCheck(L_4);
		L_4->___Kind = 0;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_5 = L_4;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6 = ___0_c;
		NullCheck(L_5);
		L_5->___O = L_6;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_7 = L_5;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8 = V_0;
		NullCheck(L_7);
		L_7->___U = L_8;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_9 = L_7;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10 = V_1;
		NullCheck(L_9);
		L_9->___V = L_10;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_11 = L_9;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12 = V_2;
		NullCheck(L_11);
		L_11->___W = L_12;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_13 = L_11;
		float L_14 = ___3_w;
		NullCheck(L_13);
		L_13->___A = ((float)il2cpp_codegen_multiply(L_14, (0.5f)));
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_15 = L_13;
		float L_16 = ___4_h;
		NullCheck(L_15);
		L_15->___B = ((float)il2cpp_codegen_multiply(L_16, (0.5f)));
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_17 = L_15;
		float L_18 = ___5_d;
		NullCheck(L_17);
		L_17->___C = ((float)il2cpp_codegen_multiply(L_18, (0.5f)));
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_19 = L_17;
		int32_t L_20 = ___6_s;
		NullCheck(L_19);
		L_19->___Surface = L_20;
		Builder_Add_mCED8C193B63F017AC5E35654B49F1A6F1E06A8CF(__this, L_19, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_b, float ___2_r, int32_t ___3_s, const RuntimeMethod* method) 
{
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = ___0_a;
		float L_1 = ___2_r;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2 = ___1_b;
		float L_3 = ___2_r;
		int32_t L_4 = ___3_s;
		Builder_Cone_mAC63A28D0AAFE35F676E5A01469B36833304726E(__this, L_0, L_1, L_2, L_3, L_4, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder_Cone_mAC63A28D0AAFE35F676E5A01469B36833304726E (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, float ___1_ra, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___2_b, float ___3_rb, int32_t ___4_s, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_0;
	memset((&V_0), 0, sizeof(V_0));
	float V_1 = 0.0f;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_2;
	memset((&V_2), 0, sizeof(V_2));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_3;
	memset((&V_3), 0, sizeof(V_3));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_4;
	memset((&V_4), 0, sizeof(V_4));
	Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* G_B4_0 = NULL;
	Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* G_B4_1 = NULL;
	Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* G_B4_2 = NULL;
	Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* G_B3_0 = NULL;
	Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* G_B3_1 = NULL;
	Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* G_B3_2 = NULL;
	int32_t G_B5_0 = 0;
	Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* G_B5_1 = NULL;
	Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* G_B5_2 = NULL;
	Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* G_B5_3 = NULL;
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = ___2_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_1 = ___0_a;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2;
		L_2 = V3_op_Subtraction_mBF754B8B036EFB8C8B32E2C6A9799AFC1B245482(L_0, L_1, NULL);
		V_0 = L_2;
		float L_3;
		L_3 = V3_get_Length_mE87BCCE258FFBA7CC979830CD4B263B2BD63B9DE((&V_0), NULL);
		V_1 = L_3;
		float L_4 = V_1;
		if ((!(((float)L_4) < ((float)(9.99999975E-06f)))))
		{
			goto IL_0019;
		}
	}
	{
		return;
	}

IL_0019:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_5 = V_0;
		float L_6 = V_1;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_7;
		L_7 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_5, ((float)((1.0f)/L_6)), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8;
		memset((&L_8), 0, sizeof(L_8));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_8), (0.0f), (1.0f), (0.0f), NULL);
		Builder_Basis_mCC3825AC627F97D83F0DF37825C687E62556B6EF(L_7, L_8, (&V_2), (&V_3), (&V_4), NULL);
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_9 = (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6*)il2cpp_codegen_object_new(Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6_il2cpp_TypeInfo_var);
		Prim__ctor_m4C75AA3E29F3B4340FC7142CFBA005FDAA8EA60B(L_9, NULL);
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_10 = L_9;
		float L_11 = ___1_ra;
		float L_12 = ___3_rb;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_13;
		L_13 = fabsf(((float)il2cpp_codegen_subtract(L_11, L_12)));
		if ((((float)L_13) < ((float)(9.99999997E-07f))))
		{
			G_B4_0 = L_10;
			G_B4_1 = L_10;
			G_B4_2 = __this;
			goto IL_005f;
		}
		G_B3_0 = L_10;
		G_B3_1 = L_10;
		G_B3_2 = __this;
	}
	{
		G_B5_0 = 2;
		G_B5_1 = G_B3_0;
		G_B5_2 = G_B3_1;
		G_B5_3 = G_B3_2;
		goto IL_0060;
	}

IL_005f:
	{
		G_B5_0 = 1;
		G_B5_1 = G_B4_0;
		G_B5_2 = G_B4_1;
		G_B5_3 = G_B4_2;
	}

IL_0060:
	{
		NullCheck(G_B5_1);
		G_B5_1->___Kind = G_B5_0;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_14 = G_B5_2;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_15 = ___0_a;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_16 = ___2_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_17;
		L_17 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_15, L_16, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_18;
		L_18 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_17, (0.5f), NULL);
		NullCheck(L_14);
		L_14->___O = L_18;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_19 = L_14;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_20 = V_2;
		NullCheck(L_19);
		L_19->___U = L_20;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_21 = L_19;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_22 = V_3;
		NullCheck(L_21);
		L_21->___V = L_22;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_23 = L_21;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_24 = V_4;
		NullCheck(L_23);
		L_23->___W = L_24;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_25 = L_23;
		float L_26 = ___1_ra;
		NullCheck(L_25);
		L_25->___A = L_26;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_27 = L_25;
		float L_28 = ___3_rb;
		NullCheck(L_27);
		L_27->___B = L_28;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_29 = L_27;
		float L_30 = V_1;
		NullCheck(L_29);
		L_29->___C = ((float)il2cpp_codegen_multiply(L_30, (0.5f)));
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_31 = L_29;
		int32_t L_32 = ___4_s;
		NullCheck(L_31);
		L_31->___Surface = L_32;
		NullCheck(G_B5_3);
		Builder_Add_mCED8C193B63F017AC5E35654B49F1A6F1E06A8CF(G_B5_3, L_31, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder_Ball_m2926A9ADD07DBA2D6A030098084DB1CB63778121 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_c, float ___1_r, int32_t ___2_s, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_0 = (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6*)il2cpp_codegen_object_new(Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6_il2cpp_TypeInfo_var);
		Prim__ctor_m4C75AA3E29F3B4340FC7142CFBA005FDAA8EA60B(L_0, NULL);
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_1 = L_0;
		NullCheck(L_1);
		L_1->___Kind = 3;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_2 = L_1;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_3 = ___0_c;
		NullCheck(L_2);
		L_2->___O = L_3;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_4 = L_2;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_5;
		memset((&L_5), 0, sizeof(L_5));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_5), (1.0f), (0.0f), (0.0f), NULL);
		NullCheck(L_4);
		L_4->___U = L_5;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_6 = L_4;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_7;
		memset((&L_7), 0, sizeof(L_7));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_7), (0.0f), (1.0f), (0.0f), NULL);
		NullCheck(L_6);
		L_6->___V = L_7;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_8 = L_6;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_9;
		memset((&L_9), 0, sizeof(L_9));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_9), (0.0f), (0.0f), (1.0f), NULL);
		NullCheck(L_8);
		L_8->___W = L_9;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_10 = L_8;
		float L_11 = ___1_r;
		NullCheck(L_10);
		L_10->___A = L_11;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_12 = L_10;
		int32_t L_13 = ___2_s;
		NullCheck(L_12);
		L_12->___Surface = L_13;
		Builder_Add_mCED8C193B63F017AC5E35654B49F1A6F1E06A8CF(__this, L_12, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder_Dish_mCA47CAB74FD0464BEEF7DF5630E487EC9CAD4A91 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_vertex, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_facing, float ___2_aperture, float ___3_sphereR, int32_t ___4_s, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_0;
	memset((&V_0), 0, sizeof(V_0));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_1;
	memset((&V_1), 0, sizeof(V_1));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_2;
	memset((&V_2), 0, sizeof(V_2));
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = ___1_facing;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_1;
		L_1 = V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892(L_0, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2;
		memset((&L_2), 0, sizeof(L_2));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_2), (0.0f), (1.0f), (0.0f), NULL);
		Builder_Basis_mCC3825AC627F97D83F0DF37825C687E62556B6EF(L_1, L_2, (&V_0), (&V_1), (&V_2), NULL);
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_3 = (Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6*)il2cpp_codegen_object_new(Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6_il2cpp_TypeInfo_var);
		Prim__ctor_m4C75AA3E29F3B4340FC7142CFBA005FDAA8EA60B(L_3, NULL);
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_4 = L_3;
		NullCheck(L_4);
		L_4->___Kind = 4;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_5 = L_4;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6 = ___0_vertex;
		NullCheck(L_5);
		L_5->___O = L_6;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_7 = L_5;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8 = V_0;
		NullCheck(L_7);
		L_7->___U = L_8;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_9 = L_7;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10 = V_1;
		NullCheck(L_9);
		L_9->___V = L_10;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_11 = L_9;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12 = V_2;
		NullCheck(L_11);
		L_11->___W = L_12;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_13 = L_11;
		float L_14 = ___2_aperture;
		NullCheck(L_13);
		L_13->___A = L_14;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_15 = L_13;
		float L_16 = ___3_sphereR;
		float L_17 = ___2_aperture;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_18;
		L_18 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA(L_16, ((float)il2cpp_codegen_multiply(L_17, (1.04999995f))), NULL);
		NullCheck(L_15);
		L_15->___C = L_18;
		Prim_t852FD29D7AD764BD431ABA4ADDED46D2BF6049D6* L_19 = L_15;
		int32_t L_20 = ___4_s;
		NullCheck(L_19);
		L_19->___Surface = L_20;
		Builder_Add_mCED8C193B63F017AC5E35654B49F1A6F1E06A8CF(__this, L_19, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder_Wing_m4000246F5FF33DAB01DA6B33EB1C083BB206263C (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* __this, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_root, float ___1_dir, int32_t ___2_panels, float ___3_pw, float ___4_ph, float ___5_gap, float ___6_tiltDeg, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_1;
	memset((&V_1), 0, sizeof(V_1));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_2;
	memset((&V_2), 0, sizeof(V_2));
	float V_3 = 0.0f;
	int32_t V_4 = 0;
	float V_5 = 0.0f;
	float V_6 = 0.0f;
	{
		float L_0 = ___6_tiltDeg;
		V_0 = ((float)(((float)il2cpp_codegen_multiply(L_0, (3.14159274f)))/(180.0f)));
		float L_1 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_2;
		L_2 = sin(((double)L_1));
		float L_3 = V_0;
		double L_4;
		L_4 = cos(((double)L_3));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&V_1), (0.0f), ((float)L_2), ((float)L_4), NULL);
		float L_5 = V_0;
		double L_6;
		L_6 = cos(((double)L_5));
		float L_7 = V_0;
		double L_8;
		L_8 = sin(((double)L_7));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&V_2), (0.0f), ((float)L_6), ((-((float)L_8))), NULL);
		float L_9 = ___5_gap;
		V_3 = ((float)il2cpp_codegen_multiply(L_9, (2.5f)));
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10 = ___0_root;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_11 = ___0_root;
		float L_12 = ___1_dir;
		float L_13 = V_3;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_14;
		memset((&L_14), 0, sizeof(L_14));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_14), ((float)il2cpp_codegen_multiply(L_12, L_13)), (0.0f), (0.0f), NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_15;
		L_15 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_11, L_14, NULL);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(__this, L_10, L_15, (0.0140000004f), 5, NULL);
		V_4 = 0;
		goto IL_0130;
	}

IL_007e:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_16 = ___0_root;
		float L_17 = L_16.___X;
		float L_18 = ___1_dir;
		float L_19 = V_3;
		float L_20 = ___3_pw;
		int32_t L_21 = V_4;
		float L_22 = ___3_pw;
		float L_23 = ___5_gap;
		V_5 = ((float)il2cpp_codegen_add(L_17, ((float)il2cpp_codegen_multiply(L_18, ((float)il2cpp_codegen_add(((float)il2cpp_codegen_add(L_19, ((float)il2cpp_codegen_multiply(L_20, (0.5f))))), ((float)il2cpp_codegen_multiply(((float)L_21), ((float)il2cpp_codegen_add(L_22, L_23))))))))));
		float L_24 = V_5;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_25 = ___0_root;
		float L_26 = L_25.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_27 = ___0_root;
		float L_28 = L_27.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_29;
		memset((&L_29), 0, sizeof(L_29));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_29), L_24, L_26, L_28, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_30 = V_1;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_31 = V_2;
		float L_32 = ___3_pw;
		float L_33 = ___4_ph;
		Builder_Slab_mAA7D3ABE211E7F908653A28B993A9888D6E4C9BE(__this, L_29, L_30, L_31, L_32, L_33, (0.0219999999f), 2, NULL);
		int32_t L_34 = V_4;
		if ((((int32_t)L_34) <= ((int32_t)0)))
		{
			goto IL_012a;
		}
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_35 = ___0_root;
		float L_36 = L_35.___X;
		float L_37 = ___1_dir;
		float L_38 = V_3;
		int32_t L_39 = V_4;
		float L_40 = ___3_pw;
		float L_41 = ___5_gap;
		float L_42 = ___5_gap;
		V_6 = ((float)il2cpp_codegen_add(L_36, ((float)il2cpp_codegen_multiply(L_37, ((float)il2cpp_codegen_subtract(((float)il2cpp_codegen_add(L_38, ((float)il2cpp_codegen_multiply(((float)L_39), ((float)il2cpp_codegen_add(L_40, L_41)))))), ((float)il2cpp_codegen_multiply(L_42, (0.5f)))))))));
		float L_43 = V_6;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_44 = ___0_root;
		float L_45 = L_44.___Y;
		float L_46 = ___4_ph;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_47 = ___0_root;
		float L_48 = L_47.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_49;
		memset((&L_49), 0, sizeof(L_49));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_49), L_43, ((float)il2cpp_codegen_subtract(L_45, ((float)il2cpp_codegen_multiply(L_46, (0.300000012f))))), L_48, NULL);
		float L_50 = V_6;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_51 = ___0_root;
		float L_52 = L_51.___Y;
		float L_53 = ___4_ph;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_54 = ___0_root;
		float L_55 = L_54.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_56;
		memset((&L_56), 0, sizeof(L_56));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_56), L_50, ((float)il2cpp_codegen_add(L_52, ((float)il2cpp_codegen_multiply(L_53, (0.300000012f))))), L_55, NULL);
		Builder_Rod_mAF09CB8EDDE258D03C8C7E608AD400CF5A8B093B(__this, L_49, L_56, (0.00899999961f), 5, NULL);
	}

IL_012a:
	{
		int32_t L_57 = V_4;
		V_4 = ((int32_t)il2cpp_codegen_add(L_57, 1));
	}

IL_0130:
	{
		int32_t L_58 = V_4;
		int32_t L_59 = ___2_panels;
		if ((((int32_t)L_58) < ((int32_t)L_59)))
		{
			goto IL_007e;
		}
	}
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder_Basis_mCC3825AC627F97D83F0DF37825C687E62556B6EF (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_w, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_upHint, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* ___2_u, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* ___3_v, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* ___4_ww, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V_0;
	memset((&V_0), 0, sizeof(V_0));
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D G_B3_0;
	memset((&G_B3_0), 0, sizeof(G_B3_0));
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_0 = ___4_ww;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_1 = ___0_w;
		*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_0 = L_1;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2 = ___1_upHint;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_3 = ___0_w;
		float L_4;
		L_4 = V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355(L_2, L_3, NULL);
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_5;
		L_5 = fabsf(L_4);
		if ((((float)L_5) > ((float)(0.949999988f))))
		{
			goto IL_001e;
		}
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6 = ___1_upHint;
		G_B3_0 = L_6;
		goto IL_0032;
	}

IL_001e:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_7;
		memset((&L_7), 0, sizeof(L_7));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_7), (1.0f), (0.0f), (0.0f), NULL);
		G_B3_0 = L_7;
	}

IL_0032:
	{
		V_0 = G_B3_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_8 = ___2_u;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_9 = V_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10 = ___0_w;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_11;
		L_11 = V3_Cross_m88C432D75F76D3BCA973DF38C923E355B02048F3(L_9, L_10, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12;
		L_12 = V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892(L_11, NULL);
		*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_8 = L_12;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_13 = ___3_v;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_14 = ___0_w;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* L_15 = ___2_u;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_16 = (*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_15);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_17;
		L_17 = V3_Cross_m88C432D75F76D3BCA973DF38C923E355B02048F3(L_14, L_16, NULL);
		*(V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*)L_13 = L_17;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Builder__ctor_m0F97E24000AE731D157CC3CE7CC858AE13978122 (Builder_t326920536BAF2DB1F02B8E6A66D9E0B87ED0CBCD* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1__ctor_mCB861F16F94DDD8BB0B8397D76C21FEE49D42687_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE* L_0 = (List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE*)il2cpp_codegen_object_new(List_1_t7E5113FBE31BDE798A56DE973C930224CBB8A7FE_il2cpp_TypeInfo_var);
		List_1__ctor_mCB861F16F94DDD8BB0B8397D76C21FEE49D42687(L_0, List_1__ctor_mCB861F16F94DDD8BB0B8397D76C21FEE49D42687_RuntimeMethod_var);
		__this->___Parts = L_0;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___Parts), (void*)L_0);
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
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* __this, float ___0_x, float ___1_y, float ___2_z, const RuntimeMethod* method) 
{
	{
		float L_0 = ___0_x;
		__this->___X = L_0;
		float L_1 = ___1_y;
		__this->___Y = L_1;
		float L_2 = ___2_z;
		__this->___Z = L_2;
		return;
	}
}
IL2CPP_EXTERN_C  void V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582_AdjustorThunk (RuntimeObject* __this, float ___0_x, float ___1_y, float ___2_z, const RuntimeMethod* method)
{
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*>(__this + _offset);
	V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582(_thisAdjusted, ___0_x, ___1_y, ___2_z, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float V3_get_Length_mE87BCCE258FFBA7CC979830CD4B263B2BD63B9DE (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		float L_0 = __this->___X;
		float L_1 = __this->___X;
		float L_2 = __this->___Y;
		float L_3 = __this->___Y;
		float L_4 = __this->___Z;
		float L_5 = __this->___Z;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_6;
		L_6 = sqrt(((double)((float)il2cpp_codegen_add(((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(L_0, L_1)), ((float)il2cpp_codegen_multiply(L_2, L_3)))), ((float)il2cpp_codegen_multiply(L_4, L_5))))));
		return ((float)L_6);
	}
}
IL2CPP_EXTERN_C  float V3_get_Length_mE87BCCE258FFBA7CC979830CD4B263B2BD63B9DE_AdjustorThunk (RuntimeObject* __this, const RuntimeMethod* method)
{
	V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D*>(__this + _offset);
	float _returnValue;
	_returnValue = V3_get_Length_mE87BCCE258FFBA7CC979830CD4B263B2BD63B9DE(_thisAdjusted, method);
	return _returnValue;
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_b, const RuntimeMethod* method) 
{
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = ___0_a;
		float L_1 = L_0.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2 = ___1_b;
		float L_3 = L_2.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_4 = ___0_a;
		float L_5 = L_4.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6 = ___1_b;
		float L_7 = L_6.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8 = ___0_a;
		float L_9 = L_8.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10 = ___1_b;
		float L_11 = L_10.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12;
		memset((&L_12), 0, sizeof(L_12));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_12), ((float)il2cpp_codegen_add(L_1, L_3)), ((float)il2cpp_codegen_add(L_5, L_7)), ((float)il2cpp_codegen_add(L_9, L_11)), NULL);
		return L_12;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_op_Subtraction_mBF754B8B036EFB8C8B32E2C6A9799AFC1B245482 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_b, const RuntimeMethod* method) 
{
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = ___0_a;
		float L_1 = L_0.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2 = ___1_b;
		float L_3 = L_2.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_4 = ___0_a;
		float L_5 = L_4.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6 = ___1_b;
		float L_7 = L_6.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8 = ___0_a;
		float L_9 = L_8.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10 = ___1_b;
		float L_11 = L_10.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12;
		memset((&L_12), 0, sizeof(L_12));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_12), ((float)il2cpp_codegen_subtract(L_1, L_3)), ((float)il2cpp_codegen_subtract(L_5, L_7)), ((float)il2cpp_codegen_subtract(L_9, L_11)), NULL);
		return L_12;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_op_UnaryNegation_mC104632D789B94979F0AAE197F1A89BDC3773D14 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, const RuntimeMethod* method) 
{
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = ___0_a;
		float L_1 = L_0.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2 = ___0_a;
		float L_3 = L_2.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_4 = ___0_a;
		float L_5 = L_4.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6;
		memset((&L_6), 0, sizeof(L_6));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_6), ((-L_1)), ((-L_3)), ((-L_5)), NULL);
		return L_6;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, float ___1_s, const RuntimeMethod* method) 
{
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = ___0_a;
		float L_1 = L_0.___X;
		float L_2 = ___1_s;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_3 = ___0_a;
		float L_4 = L_3.___Y;
		float L_5 = ___1_s;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6 = ___0_a;
		float L_7 = L_6.___Z;
		float L_8 = ___1_s;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_9;
		memset((&L_9), 0, sizeof(L_9));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_9), ((float)il2cpp_codegen_multiply(L_1, L_2)), ((float)il2cpp_codegen_multiply(L_4, L_5)), ((float)il2cpp_codegen_multiply(L_7, L_8)), NULL);
		return L_9;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float V3_Dot_m5BC51356D41E27A7E7E7DA93336B833A0ED66355 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_b, const RuntimeMethod* method) 
{
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = ___0_a;
		float L_1 = L_0.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2 = ___1_b;
		float L_3 = L_2.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_4 = ___0_a;
		float L_5 = L_4.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6 = ___1_b;
		float L_7 = L_6.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8 = ___0_a;
		float L_9 = L_8.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10 = ___1_b;
		float L_11 = L_10.___Z;
		return ((float)il2cpp_codegen_add(((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(L_1, L_3)), ((float)il2cpp_codegen_multiply(L_5, L_7)))), ((float)il2cpp_codegen_multiply(L_9, L_11))));
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_Cross_m88C432D75F76D3BCA973DF38C923E355B02048F3 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_b, const RuntimeMethod* method) 
{
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = ___0_a;
		float L_1 = L_0.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2 = ___1_b;
		float L_3 = L_2.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_4 = ___0_a;
		float L_5 = L_4.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6 = ___1_b;
		float L_7 = L_6.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8 = ___0_a;
		float L_9 = L_8.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10 = ___1_b;
		float L_11 = L_10.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12 = ___0_a;
		float L_13 = L_12.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_14 = ___1_b;
		float L_15 = L_14.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_16 = ___0_a;
		float L_17 = L_16.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_18 = ___1_b;
		float L_19 = L_18.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_20 = ___0_a;
		float L_21 = L_20.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_22 = ___1_b;
		float L_23 = L_22.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_24;
		memset((&L_24), 0, sizeof(L_24));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_24), ((float)il2cpp_codegen_subtract(((float)il2cpp_codegen_multiply(L_1, L_3)), ((float)il2cpp_codegen_multiply(L_5, L_7)))), ((float)il2cpp_codegen_subtract(((float)il2cpp_codegen_multiply(L_9, L_11)), ((float)il2cpp_codegen_multiply(L_13, L_15)))), ((float)il2cpp_codegen_subtract(((float)il2cpp_codegen_multiply(L_17, L_19)), ((float)il2cpp_codegen_multiply(L_21, L_23)))), NULL);
		return L_24;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_Mul_mF80A0EED832B6F3269918C7C10FA91E36C56F5B8 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_b, const RuntimeMethod* method) 
{
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = ___0_a;
		float L_1 = L_0.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2 = ___1_b;
		float L_3 = L_2.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_4 = ___0_a;
		float L_5 = L_4.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6 = ___1_b;
		float L_7 = L_6.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_8 = ___0_a;
		float L_9 = L_8.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10 = ___1_b;
		float L_11 = L_10.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12;
		memset((&L_12), 0, sizeof(L_12));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_12), ((float)il2cpp_codegen_multiply(L_1, L_3)), ((float)il2cpp_codegen_multiply(L_5, L_7)), ((float)il2cpp_codegen_multiply(L_9, L_11)), NULL);
		return L_12;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_Min_mD303066D8BA6D62D502E1D8BD1B22E60AD952D53 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_b, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = ___0_a;
		float L_1 = L_0.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2 = ___1_b;
		float L_3 = L_2.___X;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_4;
		L_4 = Math_Min_mE913811A2F7566294BF4649A434282634E7254B3(L_1, L_3, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_5 = ___0_a;
		float L_6 = L_5.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_7 = ___1_b;
		float L_8 = L_7.___Y;
		float L_9;
		L_9 = Math_Min_mE913811A2F7566294BF4649A434282634E7254B3(L_6, L_8, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10 = ___0_a;
		float L_11 = L_10.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12 = ___1_b;
		float L_13 = L_12.___Z;
		float L_14;
		L_14 = Math_Min_mE913811A2F7566294BF4649A434282634E7254B3(L_11, L_13, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_15;
		memset((&L_15), 0, sizeof(L_15));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_15), L_4, L_9, L_14, NULL);
		return L_15;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_Max_m0E6A19FFD4652EF0A8D51250B6A5D96A07E08925 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_b, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = ___0_a;
		float L_1 = L_0.___X;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2 = ___1_b;
		float L_3 = L_2.___X;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		float L_4;
		L_4 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA(L_1, L_3, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_5 = ___0_a;
		float L_6 = L_5.___Y;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_7 = ___1_b;
		float L_8 = L_7.___Y;
		float L_9;
		L_9 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA(L_6, L_8, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_10 = ___0_a;
		float L_11 = L_10.___Z;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_12 = ___1_b;
		float L_13 = L_12.___Z;
		float L_14;
		L_14 = Math_Max_mB55ACEA482E7F67E61496C4C7C54FE0BB7BE78EA(L_11, L_13, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_15;
		memset((&L_15), 0, sizeof(L_15));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_15), L_4, L_9, L_14, NULL);
		return L_15;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_Lerp_mB6067E0973AEFE35323954D146ED580F05E2D125 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___1_b, float ___2_t, const RuntimeMethod* method) 
{
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_0 = ___0_a;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_1 = ___1_b;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2 = ___0_a;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_3;
		L_3 = V3_op_Subtraction_mBF754B8B036EFB8C8B32E2C6A9799AFC1B245482(L_1, L_2, NULL);
		float L_4 = ___2_t;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_5;
		L_5 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_3, L_4, NULL);
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_6;
		L_6 = V3_op_Addition_m11C86B383879C89C18DEFCA143C0BEE4E07C01D2(L_0, L_5, NULL);
		return L_6;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D V3_Norm_m0F24A35DF46963B71AC41C869241E09BE32D9892 (V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D ___0_a, const RuntimeMethod* method) 
{
	float V_0 = 0.0f;
	{
		float L_0;
		L_0 = V3_get_Length_mE87BCCE258FFBA7CC979830CD4B263B2BD63B9DE((&___0_a), NULL);
		V_0 = L_0;
		float L_1 = V_0;
		if ((((float)L_1) > ((float)(9.99999972E-10f))))
		{
			goto IL_0025;
		}
	}
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_2;
		memset((&L_2), 0, sizeof(L_2));
		V3__ctor_m232CFBC12D060647586C9038543D10B62ADBF582((&L_2), (0.0f), (1.0f), (0.0f), NULL);
		return L_2;
	}

IL_0025:
	{
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_3 = ___0_a;
		float L_4 = V_0;
		V3_tF4853EBBC51A8AB100A79EF7F448E6FA9089868D L_5;
		L_5 = V3_op_Multiply_mBB40267140B3E1907464D25059487672B10C64CB(L_3, ((float)((1.0f)/L_4)), NULL);
		return L_5;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR RuntimeObject* PayloadForms_get_Ids_mC28791119EB2FF88D4CDBDB1C881C351BDC5E17E_inline (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		il2cpp_codegen_runtime_class_init_inline(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_0 = ((PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_StaticFields*)il2cpp_codegen_static_fields_for(PayloadForms_t94A88DA0A27783B58FAA021E4618AD4CE1A3373D_il2cpp_TypeInfo_var))->___Known;
		return (RuntimeObject*)L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR RuntimeObject* Enumerator_get_Current_m6330F15D18EE4F547C05DF9BF83C5EB710376027_gshared_inline (Enumerator_t9473BAB568A27E2339D48C1F91319E0F6D244D7A* __this, const RuntimeMethod* method) 
{
	{
		RuntimeObject* L_0 = __this->____current;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t List_1_get_Count_m4407E4C389F22B8CEC282C15D56516658746C383_gshared_inline (List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->____size;
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
