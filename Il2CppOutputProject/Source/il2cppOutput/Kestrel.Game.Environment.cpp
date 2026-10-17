#include "pch-cpp.hpp"





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

struct IReadOnlyList_1_t3DA86A967B1361A187E0FD611FC1F442964E6B58;
struct List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE;
struct CharU5BU5D_t799905CF001DD5F13F7DBB310181FC4D8B7D0AAB;
struct ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389;
struct DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE;
struct Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C;
struct IntPtrU5BU5D_tFD177F8C806A6921AD7150264CCC62FA00CAD832;
struct StackTraceU5BU5D_t32FBCB20930EAF5BAE3F450FF75228E5450DA0DF;
struct StarPointU5BU5D_t5585090BE6294B1F722AA45E22DF30AB449E529B;
struct StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248;
struct TypeU5BU5D_t97234E1129B564EB38B8D85CAC2AD8B5B9522FFB;
struct Binder_t91BFCE95A7057FADF4D8A1A342AFE52872246235;
struct Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184;
struct CancellationTokenSource_tAAE1E0033BCFC233801F8CB4CED5C852B350CB7B;
struct Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3;
struct GameObject_t76FEDD663AB33C991A9C9A23129337651094216F;
struct IDictionary_t6D03155AF1FA9083817AA5B6AD7DEEACC26AB220;
struct Light_t1E68479B7782AF2050FAA02A5DC612FD034F18F3;
struct Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3;
struct MemberFilter_tF644F1AE82F611B677CE1964D5A3277DDA21D553;
struct MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71;
struct Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C;
struct PropertyInfo_t;
struct SafeSerializationManager_tCBB85B95DFD1634237140CD892E82D06ECB3F5E6;
struct Shader_tADC867D36B7876EE22427FAA2CE485105F4EE692;
struct SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0;
struct SpecPortrait_t39D15BCADE1416802DC331B616A49EFBFE4750DC;
struct SpecSky_t97E08E73018D0D6E04C5AB64D06C65765C9444EE;
struct String_t;
struct TextAsset_t2C64E93DA366D9DE5A8209E1802FA4884AC1BD69;
struct Texture_t791CBB51219779964E0E8A2ED7C1AA5F92A4A700;
struct Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4;
struct TodPalette_tAB1B213C4DCFC64D0D05FB909B64DCDFC89D5A5B;
struct TodPalettes_tB538FA767CD216F7FE97CE94C9145663202A898E;
struct Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1;
struct Type_t;
struct VisualSpecData_tE23E510D926948561B6E3C7A1B4F65FC480434A5;
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915;
struct Wash_t5331DFD3DC49F7853E64611FA43BB9DA9F585498;
struct CameraCallback_t844E527BFE37BC0495E7F67993E43C07642DA9DD;

IL2CPP_EXTERN_C RuntimeClass* Application_tDB03BE91CDF0ACA614A5E0B67CFB77C44EB19B21_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Debug_t8394C7EEAECA3689C2C9B9DE9C7166D73596276F_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Exception_t_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IReadOnlyCollection_1_tF04B3A2A0FC144BE564160A18D95B937D9DB089E_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IReadOnlyList_1_t3DA86A967B1361A187E0FD611FC1F442964E6B58_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Path_t8A38A801D0219E8209C1B1D90D82D4D755D998BC_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* SkySun_t55412B2FF59478EDEEE01F79F3B0E738516396BD_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeField* U3CPrivateImplementationDetailsU3E_t73601B3260B603D2A168CBC6A785CD2B6F85B460____3F1431B31172B4BE5418BDA2F4063F512F2F7A7CFDCE81A60B131792B5F7F694_FieldInfo_var;
IL2CPP_EXTERN_C String_t* _stringLiteral06EA5078C0497FD5E98F2AC8A4ACBBF3AE62DDBC;
IL2CPP_EXTERN_C String_t* _stringLiteral0DB39025454CDDC2BE10F219994530C7C0F0FEE8;
IL2CPP_EXTERN_C String_t* _stringLiteral145EC6B8C2BA623D11BDD6955CB20C5B0C766445;
IL2CPP_EXTERN_C String_t* _stringLiteral14A85563EBD186910C075E2F651ADF038BCE4507;
IL2CPP_EXTERN_C String_t* _stringLiteral16571D1FE92CF04A8148F1652D0B353AD4471457;
IL2CPP_EXTERN_C String_t* _stringLiteral173E461F0DC20C90ABC76BD2F0A45B5DBCA33335;
IL2CPP_EXTERN_C String_t* _stringLiteral286F5A1B75EBF2E4DAD1784902DE57222F307786;
IL2CPP_EXTERN_C String_t* _stringLiteral2E15C8A32B8D7E0A09FD8A1C5B39D9A2A2BD499A;
IL2CPP_EXTERN_C String_t* _stringLiteral32EFDDFEAA75BD8C9B227E8BC9CCE4B7268C0716;
IL2CPP_EXTERN_C String_t* _stringLiteral35C1F12C5BBBFC1B19520B74D959C91FA2FFD16D;
IL2CPP_EXTERN_C String_t* _stringLiteral362D46D698B33ACEE2C21206D012143EA884291E;
IL2CPP_EXTERN_C String_t* _stringLiteral379BC7677CE810FF72085C7A382C70B00640541D;
IL2CPP_EXTERN_C String_t* _stringLiteral382032B1217B79CEF593C94296F57E9FF1FCD22E;
IL2CPP_EXTERN_C String_t* _stringLiteral4BD3052BB1EC7598F0952B3FA0F49095189128A3;
IL2CPP_EXTERN_C String_t* _stringLiteral516C5BEFDA03D6A9E669102B2FE72C5AD0CD6E56;
IL2CPP_EXTERN_C String_t* _stringLiteral57B5D0F93C2D506E618E23187302C0FADE813B93;
IL2CPP_EXTERN_C String_t* _stringLiteral598763C97CAEC44F4B760460963CD4730E60B870;
IL2CPP_EXTERN_C String_t* _stringLiteral59E9A90ACEB12BE1578CBAE545691B1979987312;
IL2CPP_EXTERN_C String_t* _stringLiteral682657B19B8755087EB65BB148A26CA596BDE3C9;
IL2CPP_EXTERN_C String_t* _stringLiteral6EEC04A3DFB247A18B195A7D12B42F30FFB94CAD;
IL2CPP_EXTERN_C String_t* _stringLiteral787389110EAF5516339BFA7A1DB299BF18596896;
IL2CPP_EXTERN_C String_t* _stringLiteral7CA8FDA9E34615850FDCF919A6FEC0033FC753A4;
IL2CPP_EXTERN_C String_t* _stringLiteral8298264B55E8E831493E62497185C522B30F5D88;
IL2CPP_EXTERN_C String_t* _stringLiteral8EC2A8CF93EE6CCEE33C770888DD7080CDAC60E1;
IL2CPP_EXTERN_C String_t* _stringLiteral9C14B5BA0DE646558EC300FF192C1B945F94D345;
IL2CPP_EXTERN_C String_t* _stringLiteral9F2140104281DD17C0E174E25ACB6E50A862FA7F;
IL2CPP_EXTERN_C String_t* _stringLiteralA2D7FD624675BA3D65DD239C532166DF399D7A7E;
IL2CPP_EXTERN_C String_t* _stringLiteralA44A39671D4B7FA8FBE50D795EAB52248D5C5469;
IL2CPP_EXTERN_C String_t* _stringLiteralA6AF09A2719F5DE17063C132A6811819EF06AC36;
IL2CPP_EXTERN_C String_t* _stringLiteralA897E8AE058B1C0A5807FF9F03EB98A76BEAC8D2;
IL2CPP_EXTERN_C String_t* _stringLiteralB1957EC98586D602AFDF307B9597C5FFD6178D65;
IL2CPP_EXTERN_C String_t* _stringLiteralB9AEECB59BF43D9ACA81E7B9AEF765A7DB4BA263;
IL2CPP_EXTERN_C String_t* _stringLiteralBB7588CFAD6537BAD69E10A3C337B7FF5790D51A;
IL2CPP_EXTERN_C String_t* _stringLiteralBB816673BA1055A7FE7842C8E1DC06FAC9CCEA45;
IL2CPP_EXTERN_C String_t* _stringLiteralBD5B20F7F6017F0873E4EF984CB16D9DC6708E3E;
IL2CPP_EXTERN_C String_t* _stringLiteralD7AD6BB90087503B8D40592A56FDBB40E5627C3A;
IL2CPP_EXTERN_C String_t* _stringLiteralE02BF054B0244C35F8DF7A02F4B8E98D37929BB1;
IL2CPP_EXTERN_C String_t* _stringLiteralED73DBABE38E94896AF0E57AD10F86530B11BBA4;
IL2CPP_EXTERN_C String_t* _stringLiteralF3C2A64A9C8933B4A8FBBDAFF40694249C19FAC9;
IL2CPP_EXTERN_C String_t* _stringLiteralF7C6D910CA12BFA2747F6EC5B8BAE314840807FA;
IL2CPP_EXTERN_C String_t* _stringLiteralF873D6748545B7516CD56A7A617453AEC82E7C8D;
IL2CPP_EXTERN_C String_t* _stringLiteralFF22701255789DB31AE3E5235D2865849D0506EE;
IL2CPP_EXTERN_C const RuntimeMethod* Component_GetComponent_TisSkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_m73D462217630CF695C0E837C0C47D9677513B8FB_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* GameObject_AddComponent_TisSkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_m07A5EC1E86349D9E5C90BADD3D8452231C6C882C_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1_RemoveAt_m93FCD13B380DF3867D816269666754C5876AC929_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1_get_Count_m5BA8B6A985C4ECE8C3EED6F7425B52AAB9BF2870_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1_get_Item_m58FDF3017420D16492329082390353B1A031618A_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Resources_Load_TisShader_tADC867D36B7876EE22427FAA2CE485105F4EE692_m378804064185FA25C41237187B3CCEA095C05946_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Resources_Load_TisTextAsset_t2C64E93DA366D9DE5A8209E1802FA4884AC1BD69_m55E770DF81AB6D40763121667DA5E743EF1036B2_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* SkyRig_SuppressSkyPost_mC67F006FB6F2258DC110DC996760B15EF6360BC9_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Type_GetType_m71A077E0B5DA3BD1DC0AB9AE387056CFCF56F93F_RuntimeMethod_var;
struct Exception_t_marshaled_com;
struct Exception_t_marshaled_pinvoke;

struct ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389;
struct Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C;
struct ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918;
struct StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248;

IL2CPP_EXTERN_C_BEGIN
IL2CPP_EXTERN_C_END

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
struct U3CModuleU3E_tE5327D2ABD274C086FCB704901BD83C5F11A90C3 
{
};
struct List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE  : public RuntimeObject
{
	StarPointU5BU5D_t5585090BE6294B1F722AA45E22DF30AB449E529B* ____items;
	int32_t ____size;
	int32_t ____version;
	RuntimeObject* ____syncRoot;
};
struct U3CPrivateImplementationDetailsU3E_t73601B3260B603D2A168CBC6A785CD2B6F85B460  : public RuntimeObject
{
};
struct MemberInfo_t  : public RuntimeObject
{
};
struct SkySun_t55412B2FF59478EDEEE01F79F3B0E738516396BD  : public RuntimeObject
{
};
struct StarBake_t093B98A3AF956BEFF86B33AFAC36E0057B81FD93  : public RuntimeObject
{
};
struct String_t  : public RuntimeObject
{
	int32_t ____stringLength;
	Il2CppChar ____firstChar;
};
struct TodPalette_tAB1B213C4DCFC64D0D05FB909B64DCDFC89D5A5B  : public RuntimeObject
{
	String_t* ___label;
	Wash_t5331DFD3DC49F7853E64611FA43BB9DA9F585498* ___wash;
	float ___stars;
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
struct VisualSpecData_tE23E510D926948561B6E3C7A1B4F65FC480434A5  : public RuntimeObject
{
	int32_t ___SpecVersion;
	SpecPortrait_t39D15BCADE1416802DC331B616A49EFBFE4750DC* ___Portrait;
	SpecSky_t97E08E73018D0D6E04C5AB64D06C65765C9444EE* ___Sky;
	TodPalettes_tB538FA767CD216F7FE97CE94C9145663202A898E* ____tod;
};
struct Wash_t5331DFD3DC49F7853E64611FA43BB9DA9F585498  : public RuntimeObject
{
	String_t* ___space;
	String_t* ___mid;
	String_t* ___near;
	String_t* ___horizon;
	String_t* ___low;
};
struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22 
{
	bool ___m_value;
};
struct Color_tD001788D726C3A7F1379BEED0260B9591F440C1F 
{
	float ___r;
	float ___g;
	float ___b;
	float ___a;
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
struct IntPtr_t 
{
	void* ___m_value;
};
struct PropertyInfo_t  : public MemberInfo_t
{
};
struct Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 
{
	float ___x;
	float ___y;
	float ___z;
	float ___w;
};
struct Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 
{
	double ___R;
	double ___G;
	double ___B;
};
struct Single_t4530F2FF86FCB0DC29F35385CA1BD21BE294761C 
{
	float ___m_value;
};
struct SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D 
{
	double ___X;
	double ___Y;
	double ___Z;
};
struct SkyWeather_t905CF34969B44F09A45D96F10FA58C6D01AE2DA2 
{
	double ___CloudFraction;
	double ___CeilingM;
	bool ___Precip;
	bool ___Thunder;
	double ___WindDeg;
	double ___WindMs;
};
struct SkyWeather_t905CF34969B44F09A45D96F10FA58C6D01AE2DA2_marshaled_pinvoke
{
	double ___CloudFraction;
	double ___CeilingM;
	int32_t ___Precip;
	int32_t ___Thunder;
	double ___WindDeg;
	double ___WindMs;
};
struct SkyWeather_t905CF34969B44F09A45D96F10FA58C6D01AE2DA2_marshaled_com
{
	double ___CloudFraction;
	double ___CeilingM;
	int32_t ___Precip;
	int32_t ___Thunder;
	double ___WindDeg;
	double ___WindMs;
};
struct StarPoint_t8DA928CE6AE24DC5827194FA6BD3B437EC92A3CE 
{
	int32_t ___Id;
	double ___RaDeg;
	double ___DecDeg;
	double ___VMag;
	double ___BV;
	double ___Brightness;
};
struct UInt32_t1833D51FFA667B18A5AA4B8D34DE284F8495D29B 
{
	uint32_t ___m_value;
};
struct Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 
{
	float ___x;
	float ___y;
	float ___z;
};
struct Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 
{
	float ___x;
	float ___y;
	float ___z;
	float ___w;
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
struct WeatherDay_tBFA6624F16FBACC13014D8A250FFF8CD5AFA6EBC 
{
	String_t* ___siteId;
	int32_t ___dayOfYear;
	int32_t ___month;
	uint32_t ___seed;
	double ___windMs;
	double ___gustMs;
	double ___directionDeg;
	double ___tempC;
	double ___cloudFraction;
	bool ___precip;
	double ___ceilingM;
	bool ___thunderDay;
	double ___lightningKm;
	double ___shearPerSec;
	DoubleU5BU5D_tCC308475BD3B8229DB2582938669EF2F9ECC1FEE* ___shearWindMs;
};
struct WeatherDay_tBFA6624F16FBACC13014D8A250FFF8CD5AFA6EBC_marshaled_pinvoke
{
	char* ___siteId;
	int32_t ___dayOfYear;
	int32_t ___month;
	uint32_t ___seed;
	double ___windMs;
	double ___gustMs;
	double ___directionDeg;
	double ___tempC;
	double ___cloudFraction;
	int32_t ___precip;
	double ___ceilingM;
	int32_t ___thunderDay;
	double ___lightningKm;
	double ___shearPerSec;
	Il2CppSafeArray* ___shearWindMs;
};
struct WeatherDay_tBFA6624F16FBACC13014D8A250FFF8CD5AFA6EBC_marshaled_com
{
	Il2CppChar* ___siteId;
	int32_t ___dayOfYear;
	int32_t ___month;
	uint32_t ___seed;
	double ___windMs;
	double ___gustMs;
	double ___directionDeg;
	double ___tempC;
	double ___cloudFraction;
	int32_t ___precip;
	double ___ceilingM;
	int32_t ___thunderDay;
	double ___lightningKm;
	double ___shearPerSec;
	Il2CppSafeArray* ___shearWindMs;
};
#pragma pack(push, tp, 1)
struct __StaticArrayInitTypeSizeU3D48_t67B7F3118D8D2EC2ED23291C6D7BB2B8EFA446AD 
{
	union
	{
		struct
		{
			union
			{
			};
		};
		uint8_t __StaticArrayInitTypeSizeU3D48_t67B7F3118D8D2EC2ED23291C6D7BB2B8EFA446AD__padding[48];
	};
};
#pragma pack(pop, tp)
struct AmbientMode_tD34D80BFF7FD58AABB7BF9640B68FEF732C187E2 
{
	int32_t ___value__;
};
struct CameraClearFlags_t91B921013F611457A09B92EF9C6B218CECF67202 
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
struct FilterMode_t4AD57F1A3FE272D650E0E688BA044AE872BD2A34 
{
	int32_t ___value__;
};
struct FogMode_t2D73DEA78AF7257585DC9D4B1A1B0A028EDA0F39 
{
	int32_t ___value__;
};
struct HideFlags_tC514182ACEFD3B847988C45D5DB812FF6DB1BF4A 
{
	int32_t ___value__;
};
struct LightShape_t538BE3D1AD8C9B537615DAE1C77FA43E26295E91 
{
	int32_t ___value__;
};
struct Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C  : public RuntimeObject
{
	intptr_t ___m_CachedPtr;
};
struct Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_marshaled_pinvoke
{
	intptr_t ___m_CachedPtr;
};
struct Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_marshaled_com
{
	intptr_t ___m_CachedPtr;
};
struct RuntimeFieldHandle_t6E4C45B6D2EA12FC99185805A7E77527899B25C5 
{
	intptr_t ___value;
};
struct RuntimeTypeHandle_t332A452B8B6179E4469B69525D0FE82A88030F7B 
{
	intptr_t ___value;
};
struct SkyArt_t9BEFE797BE1852E9A478879F377E2884E3F7A751 
{
	Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 ___Horizon;
	Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 ___Near;
	Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 ___Space;
	double ___StarFloor;
};
struct SkyAtmosphereMode_tF077627E6BFA273290EC12129C285FD2BAC7A4C4 
{
	int32_t ___value__;
};
struct StringComparison_tE14A55CCFA001A5AC85D754179BF2888F45CC94D 
{
	int32_t ___value__;
};
struct TextureFormat_t87A73E4A3850D3410DC211676FC14B94226C1C1D 
{
	int32_t ___value__;
};
struct TextureWrapMode_tF9851343029052ED45668D1C99BAE09B2CCC13AD 
{
	int32_t ___value__;
};
struct Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3  : public Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C
{
};
struct GameObject_t76FEDD663AB33C991A9C9A23129337651094216F  : public Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C
{
};
struct Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3  : public Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C
{
};
struct Shader_tADC867D36B7876EE22427FAA2CE485105F4EE692  : public Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C
{
};
struct SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C 
{
	Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 ___Zenith;
	Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 ___Horizon;
	Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 ___Limb;
	Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 ___Ground;
	Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 ___Fog;
	Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 ___Sun;
	double ___StarAlpha;
	double ___CloudAlpha;
	double ___Haze;
	double ___Milky;
	double ___Airglow;
	double ___SunDisc;
	double ___SunGlow;
	double ___Column;
	double ___SpaceT;
	double ___SunElevationDeg;
	double ___FogStartM;
	double ___FogEndM;
	double ___Cloud0;
	double ___Cloud1;
	double ___Cloud2;
	double ___Height0;
	double ___Height1;
	double ___Height2;
	double ___WindEast;
	double ___WindNorth;
	int32_t ___Mode;
};
struct TextAsset_t2C64E93DA366D9DE5A8209E1802FA4884AC1BD69  : public Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C
{
};
struct Texture_t791CBB51219779964E0E8A2ED7C1AA5F92A4A700  : public Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C
{
};
struct Type_t  : public MemberInfo_t
{
	RuntimeTypeHandle_t332A452B8B6179E4469B69525D0FE82A88030F7B ____impl;
};
struct Behaviour_t01970CFBBA658497AE30F311C447DB0440BAB7FA  : public Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3
{
};
struct Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4  : public Texture_t791CBB51219779964E0E8A2ED7C1AA5F92A4A700
{
};
struct Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1  : public Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3
{
};
struct Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184  : public Behaviour_t01970CFBBA658497AE30F311C447DB0440BAB7FA
{
	uint32_t ___m_NonSerializedVersion;
};
struct Light_t1E68479B7782AF2050FAA02A5DC612FD034F18F3  : public Behaviour_t01970CFBBA658497AE30F311C447DB0440BAB7FA
{
	int32_t ___U3CshapeU3Ek__BackingField;
	int32_t ___m_BakedIndex;
};
struct MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71  : public Behaviour_t01970CFBBA658497AE30F311C447DB0440BAB7FA
{
	CancellationTokenSource_tAAE1E0033BCFC233801F8CB4CED5C852B350CB7B* ___m_CancellationTokenSource;
};
struct SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0  : public MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71
{
	Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* ___View;
	Light_t1E68479B7782AF2050FAA02A5DC612FD034F18F3* ___SunLight;
	String_t* ___Tier;
	String_t* ___TodId;
	SkyArt_t9BEFE797BE1852E9A478879F377E2884E3F7A751 ___Art;
	SkyWeather_t905CF34969B44F09A45D96F10FA58C6D01AE2DA2 ___Weather;
	double ___LatitudeDeg;
	double ___LongitudeDeg;
	int32_t ___Year;
	int32_t ___Month;
	int32_t ___Day;
	double ___HoursUtc;
	Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* ____mat;
	Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* ____stars;
	String_t* ____bakedTier;
	double ____altitudeM;
	bool ____artExplicit;
	bool ____artReady;
	bool ____sunReady;
	double ____elev;
	double ____clockKey;
	SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D ____sunDir;
	SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C ___U3CGradeU3Ek__BackingField;
};
struct List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE_StaticFields
{
	StarPointU5BU5D_t5585090BE6294B1F722AA45E22DF30AB449E529B* ___s_emptyArray;
};
struct U3CPrivateImplementationDetailsU3E_t73601B3260B603D2A168CBC6A785CD2B6F85B460_StaticFields
{
	__StaticArrayInitTypeSizeU3D48_t67B7F3118D8D2EC2ED23291C6D7BB2B8EFA446AD ___3F1431B31172B4BE5418BDA2F4063F512F2F7A7CFDCE81A60B131792B5F7F694;
};
struct SkySun_t55412B2FF59478EDEEE01F79F3B0E738516396BD_StaticFields
{
	float ___U3CElevationDegU3Ek__BackingField;
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
struct Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974_StaticFields
{
	Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 ___identityQuaternion;
};
struct Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_StaticFields
{
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___zeroVector;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___oneVector;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___upVector;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___downVector;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___leftVector;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___rightVector;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___forwardVector;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___backVector;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___positiveInfinityVector;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___negativeInfinityVector;
};
struct Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3_StaticFields
{
	Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 ___zeroVector;
	Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 ___oneVector;
	Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 ___positiveInfinityVector;
	Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 ___negativeInfinityVector;
};
struct Exception_t_StaticFields
{
	RuntimeObject* ___s_EDILock;
};
struct Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_StaticFields
{
	int32_t ___OffsetOfInstanceIDInCPlusPlusObject;
};
struct Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3_StaticFields
{
	int32_t ___k_ColorId;
	int32_t ___k_MainTexId;
};
struct Texture_t791CBB51219779964E0E8A2ED7C1AA5F92A4A700_StaticFields
{
	int32_t ___GenerateAllMips;
};
struct Type_t_StaticFields
{
	Binder_t91BFCE95A7057FADF4D8A1A342AFE52872246235* ___s_defaultBinder;
	Il2CppChar ___Delimiter;
	TypeU5BU5D_t97234E1129B564EB38B8D85CAC2AD8B5B9522FFB* ___EmptyTypes;
	RuntimeObject* ___Missing;
	MemberFilter_tF644F1AE82F611B677CE1964D5A3277DDA21D553* ___FilterAttribute;
	MemberFilter_tF644F1AE82F611B677CE1964D5A3277DDA21D553* ___FilterName;
	MemberFilter_tF644F1AE82F611B677CE1964D5A3277DDA21D553* ___FilterNameIgnoreCase;
};
struct Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184_StaticFields
{
	CameraCallback_t844E527BFE37BC0495E7F67993E43C07642DA9DD* ___onPreCull;
	CameraCallback_t844E527BFE37BC0495E7F67993E43C07642DA9DD* ___onPreRender;
	CameraCallback_t844E527BFE37BC0495E7F67993E43C07642DA9DD* ___onPostRender;
};
struct SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields
{
	int32_t ___ZenithId;
	int32_t ___HorizonId;
	int32_t ___LimbId;
	int32_t ___GroundId;
	int32_t ___SunColorId;
	int32_t ___SunDirId;
	int32_t ___ParamsId;
	int32_t ___MilkyId;
	int32_t ___CloudAId;
	int32_t ___CloudHId;
	int32_t ___WindId;
	int32_t ___Eq0Id;
	int32_t ___Eq1Id;
	int32_t ___Eq2Id;
	int32_t ___GalNId;
	int32_t ___StarTexId;
	int32_t ___StarsId;
	bool ____forced;
	bool ____cliChecked;
	double ____forcedLocal;
	SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* ____active;
};
#ifdef __clang__
#pragma clang diagnostic pop
#endif
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
struct ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389  : public RuntimeArray
{
	ALIGN_FIELD (8) Color_tD001788D726C3A7F1379BEED0260B9591F440C1F m_Items[1];

	inline Color_tD001788D726C3A7F1379BEED0260B9591F440C1F GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline Color_tD001788D726C3A7F1379BEED0260B9591F440C1F* GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, Color_tD001788D726C3A7F1379BEED0260B9591F440C1F value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
	}
	inline Color_tD001788D726C3A7F1379BEED0260B9591F440C1F GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline Color_tD001788D726C3A7F1379BEED0260B9591F440C1F* GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, Color_tD001788D726C3A7F1379BEED0260B9591F440C1F value)
	{
		m_Items[index] = value;
	}
};


IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* Component_GetComponent_TisRuntimeObject_m7181F81CAEC2CF53F5D2BC79B7425C16E1F80D33_gshared (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* GameObject_AddComponent_TisRuntimeObject_m69B93700FACCF372F5753371C6E8FB780800B824_gshared (GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* Resources_Load_TisRuntimeObject_mD1AF6299B14F87ED1D1A6199A51480919F7C79D7_gshared (String_t* ___0_path, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t List_1_get_Count_m5BA8B6A985C4ECE8C3EED6F7425B52AAB9BF2870_gshared_inline (List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR StarPoint_t8DA928CE6AE24DC5827194FA6BD3B437EC92A3CE List_1_get_Item_m58FDF3017420D16492329082390353B1A031618A_gshared (List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* __this, int32_t ___0_index, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void List_1_RemoveAt_m93FCD13B380DF3867D816269666754C5876AC929_gshared (List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* __this, int32_t ___0_index, const RuntimeMethod* method) ;

IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float SkySun_get_ElevationDeg_m88C47C05DAB5E5D03500C5F8E53F677A31E17113_inline (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602 (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* ___0_x, Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* ___1_y, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkySun_ApplyLocalHours_m9888C619611714B145E4637C51B646A7EF24396A (double ___0_hoursLocal, double ___1_latitudeDeg, double ___2_longitudeDeg, int32_t ___3_year, int32_t ___4_month, int32_t ___5_day, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605 (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* ___0_x, Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* ___1_y, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double SkyBodies_UtcFromLocalHours_m3ACF33FAD1AC595905E6A73B3E3E34649FA68244 (double ___0_hoursLocal, double ___1_longitudeDeg, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_Refresh_mDBEAB6290ED6115E2BC879C0A77B1620DA523745 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) ;
inline SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* Component_GetComponent_TisSkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_m73D462217630CF695C0E837C0C47D9677513B8FB (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* __this, const RuntimeMethod* method)
{
	return ((  SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* (*) (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3*, const RuntimeMethod*))Component_GetComponent_TisRuntimeObject_m7181F81CAEC2CF53F5D2BC79B7425C16E1F80D33_gshared)(__this, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* Component_get_gameObject_m57AEFBB14DB39EC476F740BA000E170355DE691B (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* __this, const RuntimeMethod* method) ;
inline SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* GameObject_AddComponent_TisSkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_m07A5EC1E86349D9E5C90BADD3D8452231C6C882C (GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* __this, const RuntimeMethod* method)
{
	return ((  SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* (*) (GameObject_t76FEDD663AB33C991A9C9A23129337651094216F*, const RuntimeMethod*))GameObject_AddComponent_TisRuntimeObject_m69B93700FACCF372F5753371C6E8FB780800B824_gshared)(__this, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_PickTier_mFE1C3A8E6EC267D5DF49DC1A4DD784FB492D3F01 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_LoadWeather_m9CEFA38CC4D4407F5E670B05BE4B7CF47B35AF31 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_SuppressSkyPost_mC67F006FB6F2258DC110DC996760B15EF6360BC9 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR SkyWeather_t905CF34969B44F09A45D96F10FA58C6D01AE2DA2 SkyWeather_From_mEC3BBC0876EC155ABA0C80CB9F4B2C637C190FB2 (WeatherDay_tBFA6624F16FBACC13014D8A250FFF8CD5AFA6EBC ___0_day, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t SystemInfo_get_systemMemorySize_m3BFE40CF5A43FEAB94F5C552A47D04ECD88B771E (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool String_IsNullOrEmpty_mEA9E3FB005AC28FE02E69FCF95A7B8456192B478 (String_t* ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR VisualSpecData_tE23E510D926948561B6E3C7A1B4F65FC480434A5* VisualSpecData_Load_mC791D3DC1CE4B9E4F82A350C91F6CFA274D34FD0 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* SkyRig_TodForSun_mB83FE28562D7B50BFEA0268AF1D05326DA333030 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR TodPalette_tAB1B213C4DCFC64D0D05FB909B64DCDFC89D5A5B* VisualSpecData_Palette_mA17CAA995B096EC32C554DB8333D97E20338E8BF (VisualSpecData_tE23E510D926948561B6E3C7A1B4F65FC480434A5* __this, String_t* ___0_id, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 Rgb_FromHex_m5B4493AE3CE25CE85C2247E98D1BBB2AA90D7B7A (String_t* ___0_hex, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR SkyArt_t9BEFE797BE1852E9A478879F377E2884E3F7A751 SkyArt_get_Neutral_m41D00FAAB2812B17D162EEA12A406303A987E85E (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double SkyBodies_SunElevationRad_mF217103BA291B956A97948403542A179DA55F6E4 (double ___0_latDeg, double ___1_lonDeg, int32_t ___2_year, int32_t ___3_month, int32_t ___4_day, double ___5_hoursUtc, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t SkyRig_DayOfYear_mDAC0DB5A12D5B64FB1909622D6313A6682216BA8 (int32_t ___0_year, int32_t ___1_month, int32_t ___2_day, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR WeatherDay_tBFA6624F16FBACC13014D8A250FFF8CD5AFA6EBC WeatherGenerator_Generate_m81762F3EB04ECFCB02FC7CEA5DD7A3DC0EE70BA5 (String_t* ___0_siteId, int32_t ___1_dayOfYear, uint32_t ___2_seed, String_t* ___3_difficulty, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR SkyWeather_t905CF34969B44F09A45D96F10FA58C6D01AE2DA2 SkyWeather_get_Clear_m0951F86F60733CA40966791E0BF16A102EF60774 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RuntimeHelpers_InitializeArray_m751372AA3F24FBF6DA9B9D687CBFA2DE436CAB9B (RuntimeArray* ___0_array, RuntimeFieldHandle_t6E4C45B6D2EA12FC99185805A7E77527899B25C5 ___1_fldHandle, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_ApplyCommandLine_mBF59C29ACCB257D543549FF7F4A73532BE4A34BD (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_EnsureMaterial_m8C085242757B7A62E4ACE2DC2D1C1483DCC742B7 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool SkyRig_SampleSun_m308A2A28229172EF98B04ADB14986A1AD2C2B949 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkySun_Publish_m0C85B44B4C354730B45B7E66EE056A1FB48B795F (double ___0_elevationRad, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_LoadArt_m45B7895F5EE4253439D8857A0FDA283732A388F6 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C SkyColor_At_m4282570A7DDEC1791A2429368F1133D5C23B4127 (double ___0_altitudeM, double ___1_sunElevationRad, SkyArt_t9BEFE797BE1852E9A478879F377E2884E3F7A751 ___2_art, SkyWeather_t905CF34969B44F09A45D96F10FA58C6D01AE2DA2 ___3_weather, String_t* ___4_status, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void SkyRig_set_Grade_m2DFB8271381DE1BAF1321B6B0E623E833031C8C0_inline (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C ___0_value, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C SkyRig_get_Grade_mB922C963C06C2B47481FCC64085AE25BA134C453_inline (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_Apply_m335778822D8B098D9CC60D14674773698488EAAD (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C ___0_g, SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D ___1_sun, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* Environment_GetCommandLineArgs_mD29CFA1CD3C84F9BD91152E70302E908114A831D (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool SkyBodies_HasNightArg_mDC38C1F5DD3FDECC5C4928838C5350B3D2014DF9 (StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* ___0_args, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_ForceTimeOfDay_mDE42607E9DE6AF0F25C108B0D689F08AB7D92B75 (double ___0_hoursLocal, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D SkyBodies_SunUnity_m208FFDA36A2CD87C7FF8D15FE87D67077F03D2D6 (double ___0_latDeg, double ___1_lonDeg, int32_t ___2_year, int32_t ___3_month, int32_t ___4_day, double ___5_hoursUtc, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Type_op_Equality_m99930A0E44E420A685FABA60E60BA1CC5FA0EBDC (Type_t* ___0_left, Type_t* ___1_right, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* Component_GetComponent_m4352437B839B9601EB007CAC1007C8E1FB8A427D (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* __this, Type_t* ___0_type, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* GameObject_AddComponent_mDF246771EC34613FA6AF0C98D443368FB43E9F36 (GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* __this, Type_t* ___0_componentType, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_SetMember_m9361B75A5116B5F4FAF6AC020CD306FFCE63E947 (Type_t* ___0_t, RuntimeObject* ___1_target, String_t* ___2_name, bool ___3_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PropertyInfo_t* Type_GetProperty_mD183124FC8A89121E8368058B327A7750B14281D (Type_t* __this, String_t* ___0_name, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool PropertyInfo_op_Inequality_mE75A4F14CC678D8A670730FBD4338C718CACB51B (PropertyInfo_t* ___0_left, PropertyInfo_t* ___1_right, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* Enum_ToObject_m7952B7BD43E48BBF79BB973BD31E7FF9E62B948E (Type_t* ___0_enumType, int32_t ___1_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Type_t* Type_GetTypeFromHandle_m6062B81682F79A4D6DF2640692EE6D9987858C57 (RuntimeTypeHandle_t332A452B8B6179E4469B69525D0FE82A88030F7B ___0_handle, const RuntimeMethod* method) ;
inline Shader_tADC867D36B7876EE22427FAA2CE485105F4EE692* Resources_Load_TisShader_tADC867D36B7876EE22427FAA2CE485105F4EE692_m378804064185FA25C41237187B3CCEA095C05946 (String_t* ___0_path, const RuntimeMethod* method)
{
	return ((  Shader_tADC867D36B7876EE22427FAA2CE485105F4EE692* (*) (String_t*, const RuntimeMethod*))Resources_Load_TisRuntimeObject_mD1AF6299B14F87ED1D1A6199A51480919F7C79D7_gshared)(___0_path, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Shader_tADC867D36B7876EE22427FAA2CE485105F4EE692* Shader_Find_m183AA54F78320212DDEC811592F98456898A41C5 (String_t* ___0_name, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool SkyRig_IsErrorShader_mC4CDA81F46C7A7F50E9979F84005B73B429AF45A (Shader_tADC867D36B7876EE22427FAA2CE485105F4EE692* ___0_shader, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Debug_LogError_mB00B2B4468EF3CAF041B038D840820FB84C924B2 (RuntimeObject* ___0_message, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Material__ctor_m7FDF47105D66D19591BE505A0C42B0F90D88C9BF (Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* __this, Shader_tADC867D36B7876EE22427FAA2CE485105F4EE692* ___0_shader, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Object_set_name_mC79E6DC8FFD72479C90F0C4CC7F42A0FEAF5AE47 (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* __this, String_t* ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Shader_tADC867D36B7876EE22427FAA2CE485105F4EE692* Material_get_shader_m8B0C11AE6F2AD7DE30AF52D3195EB716F7A71983 (Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Object_Destroy_mE97D0A766419A81296E8D4E5C23D01D3FE91ACBB (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* ___0_obj, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Object_set_hideFlags_mACB8BFC903FB3B01BBD427753E791BF28B5E33D4 (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* __this, int32_t ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RenderSettings_set_skybox_m7988CC3217B1456CA0A7294127527BFC18EEC872 (Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Camera_set_clearFlags_m66541D9CC43CBAA5FE7364A50D43CA5569FD4D93 (Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* __this, int32_t ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_BakeStars_m7E771561D418AD46D63615396CBE9FBF52240743 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1 (String_t* ___0_a, String_t* ___1_b, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* SkyRig_ReadCatalog_m9B05C52846802B3200329BAF7CD93204CA621821 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* SkyBodies_LoadStars_m7A9416B647B668B98A44700FC25C2CC0EAD786A7 (String_t* ___0_csv, String_t* ___1_tier, const RuntimeMethod* method) ;
inline int32_t List_1_get_Count_m5BA8B6A985C4ECE8C3EED6F7425B52AAB9BF2870_inline (List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* __this, const RuntimeMethod* method)
{
	return ((  int32_t (*) (List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE*, const RuntimeMethod*))List_1_get_Count_m5BA8B6A985C4ECE8C3EED6F7425B52AAB9BF2870_gshared_inline)(__this, method);
}
inline StarPoint_t8DA928CE6AE24DC5827194FA6BD3B437EC92A3CE List_1_get_Item_m58FDF3017420D16492329082390353B1A031618A (List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* __this, int32_t ___0_index, const RuntimeMethod* method)
{
	return ((  StarPoint_t8DA928CE6AE24DC5827194FA6BD3B437EC92A3CE (*) (List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE*, int32_t, const RuntimeMethod*))List_1_get_Item_m58FDF3017420D16492329082390353B1A031618A_gshared)(__this, ___0_index, method);
}
inline void List_1_RemoveAt_m93FCD13B380DF3867D816269666754C5876AC929 (List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* __this, int32_t ___0_index, const RuntimeMethod* method)
{
	((  void (*) (List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE*, int32_t, const RuntimeMethod*))List_1_RemoveAt_m93FCD13B380DF3867D816269666754C5876AC929_gshared)(__this, ___0_index, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* StarBake_Bake_m6581337B736F5EEB31BB6C44F7BB503EAD7A6DBE (RuntimeObject* ___0_stars, int32_t ___1_width, int32_t ___2_height, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Material_SetTexture_mA9F8461850AAB88F992E9C6FA6F24C2E050B83FD (Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* __this, int32_t ___0_nameID, Texture_t791CBB51219779964E0E8A2ED7C1AA5F92A4A700* ___1_value, const RuntimeMethod* method) ;
inline TextAsset_t2C64E93DA366D9DE5A8209E1802FA4884AC1BD69* Resources_Load_TisTextAsset_t2C64E93DA366D9DE5A8209E1802FA4884AC1BD69_m55E770DF81AB6D40763121667DA5E743EF1036B2 (String_t* ___0_path, const RuntimeMethod* method)
{
	return ((  TextAsset_t2C64E93DA366D9DE5A8209E1802FA4884AC1BD69* (*) (String_t*, const RuntimeMethod*))Resources_Load_TisRuntimeObject_mD1AF6299B14F87ED1D1A6199A51480919F7C79D7_gshared)(___0_path, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* TextAsset_get_text_m36846042E3CF3D9DD337BF3F8B2B1902D10C8FD9 (TextAsset_t2C64E93DA366D9DE5A8209E1802FA4884AC1BD69* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* Application_get_dataPath_m4C8412CBEE4EAAAB6711CC9BEFFA73CEE5BDBEF7 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* Catalog_FindRepoRoot_mEFA3BEDD8D988CD5669DCD08A60D512D6D5113AD (String_t* ___0_start, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* Path_Combine_m3465FB3E62CC193B0B916386921C8AA72026FD13 (String_t* ___0_path1, String_t* ___1_path2, String_t* ___2_path3, String_t* ___3_path4, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool File_Exists_m95E329ABBE3EAD6750FE1989BBA6884457136D4A (String_t* ___0_path, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* File_ReadAllText_mA4A939F853D573379F7129AFDC469B91E9747BAA (String_t* ___0_path, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* Application_get_streamingAssetsPath_mB904BCD9A7A4F18A52C175DE4A81F5DC3010CDB5 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* Path_Combine_m1ADAC05CDA2D1D61B172DF65A81E86592696BEAE (String_t* ___0_path1, String_t* ___1_path2, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* Object_get_name_mAC2F6B897CF1303BA4249B4CB55271AFACBB6392 (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t String_IndexOf_m490810CB7ADA9230AC0F8D78E213A8EFED129F55 (String_t* __this, String_t* ___0_value, int32_t ___1_comparisonType, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Color_tD001788D726C3A7F1379BEED0260B9591F440C1F SkyRig_Display_m5B3BB5D54D4BC9D33F8865240C9091D2CD32F4D4 (Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 ___0_c, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Camera_set_backgroundColor_m036FD8C316A93A0B168ACC89AFF16D396B872138 (Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* __this, Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_ApplyFog_m683B5D221A6F7E19D876507522078CA3380663D0 (SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C ___0_g, Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___1_horizon, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_AimSun_m5FAFDB6D014B5F14E368CD8C381031DD402B90C0 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D ___0_sun, SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C ___1_g, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Material_SetColor_m573C88F2FB1B5A978C53A197B414F9E9C6AC5B9A (Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* __this, int32_t ___0_nameID, Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___1_value, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Vector4__ctor_m96B2CD8B862B271F513AF0BDC2EABD58E4DBC813_inline (Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3* __this, float ___0_x, float ___1_y, float ___2_z, float ___3_w, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Material_SetVector_m44CD02D4555E2AF391C30700F0AEC36BA04CFEA7 (Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* __this, int32_t ___0_nameID, Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 ___1_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float Time_get_time_m3A271BB1B20041144AC5B7863B71AB1F0150374B (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Frames_JdUt1Parts_m600EE3044F3D0E1F3B1EA85B22EE165DF6DF13CA (int32_t ___0_year, int32_t ___1_month, int32_t ___2_day, double ___3_hoursUtc, double* ___4_jd1, double* ___5_jd2, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyBodies_EquatorialAxes_m1979046ACA70599109036BCD884367E0A7E4B1AC (double ___0_latDeg, double ___1_lonDeg, double ___2_jd1, double ___3_jd2, SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D* ___4_x, SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D* ___5_y, SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D* ___6_z, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 SkyRig_Vec_m941B9A7A51ED8C05D5C250649238DD8B2E15F055 (SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D ___0_v, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D SkyBodies_GalacticNorthEquatorial_m49B8A340BCC81620E8805E42D4675EE298751AE0 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float SkySun_NightFactor_m8D68B457C71134AFA96E7C36F0F3267125D2674D (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RenderSettings_set_ambientMode_mDC8C4F62B1BCDDBFA69E9398213B926E39D512FC (int32_t ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RenderSettings_set_ambientSkyColor_m5C39982DA79150786D1C220A98EF7925CD2714CE (Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RenderSettings_set_ambientEquatorColor_mF032192969C1E56A8A67A210BFC27C98DCB8CADB (Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RenderSettings_set_ambientGroundColor_m78EC24258C1F55C474321A66A4C73742E7082850 (Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___0_value, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Mathf_Lerp_m47EF2FFB7647BD0A1FDC26DC03E28B19812139B5_inline (float ___0_a, float ___1_b, float ___2_t, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RenderSettings_set_ambientIntensity_m27875436E47E2D28C13F362DAE9C24D0DEC4A844 (float ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RenderSettings_set_fog_m3CB80EC4A17400AF2D6D7CBE6B541E01D8E2AA0E (bool ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RenderSettings_set_fogMode_m9D67706DC8DB33B69479A6D47CEC2131E93363C0 (int32_t ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RenderSettings_set_fogColor_mBBD12EC3492195F85F583CAF4FAE4A973227639B (Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RenderSettings_set_fogStartDistance_mF53EDDD1B96FD948B5213BAC17480FEE847896CA (float ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RenderSettings_set_fogEndDistance_m3A8CA92B0A13432069104061629C25DF13BAEDDD (float ___0_value, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Vector3__ctor_m376936E6B999EF1ECBE57D990A386303E2283DE0_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* __this, float ___0_x, float ___1_y, float ___2_z, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Vector3_get_sqrMagnitude_m43C27DEC47C4811FB30AB474FF2131A963B66FC8_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_get_up_m128AF3FDC820BF59D5DE86D973E7DE3F20C3AEBA_inline (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371 (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_get_normalized_m736BBF65D5CDA7A18414370D15B4DFCC1E466F07_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_op_UnaryNegation_m5450829F333BD2A88AF9A592C4EE331661225915_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___0_a, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 Quaternion_LookRotation_mDB2CCA75B8E1AB98104F2A6E1A1EA57D0D1298D7_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___0_forward, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Transform_set_rotation_m61340DE74726CF0F9946743A727C4D444397331D (Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* __this, Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Light_set_color_m242F09E22904E0A20D5B032A0749678B0796A87C (Light_t1E68479B7782AF2050FAA02A5DC612FD034F18F3* __this, Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Light_set_intensity_mE4820C7F39F490B92ED5EA0C3AADA7C0775BE854 (Light_t1E68479B7782AF2050FAA02A5DC612FD034F18F3* __this, float ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RenderSettings_set_sun_m3331D8BCAF24DFA30A2BB1CCDA6542958896FEAD (Light_t1E68479B7782AF2050FAA02A5DC612FD034F18F3* ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 Rgb_ToSrgb_m0C7E6A1067F5AAE700CE816C919291A9A9C4E8D4 (Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Color__ctor_m3786F0D6E510D9CFA544523A955870BD2A514C8C_inline (Color_tD001788D726C3A7F1379BEED0260B9591F440C1F* __this, float ___0_r, float ___1_g, float ___2_b, float ___3_a, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Color_tD001788D726C3A7F1379BEED0260B9591F440C1F Color_get_black_mB50217951591A045844C61E7FF31EEE3FEF16737_inline (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void MonoBehaviour__ctor_m592DB0105CA0BC97AA1C5F4AD27B12D68A3B7C1E (MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t Shader_PropertyToID_mE98523D50F5656CAE89B30695C458253EB8956CA (String_t* ___0_name, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyBodies_EquirectTexel_mBCBB527630A3EA4D30E588FBCB173690E300AF0B (double ___0_raDeg, double ___1_decDeg, int32_t ___2_width, int32_t ___3_height, int32_t* ___4_x, int32_t* ___5_y, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 SkyBodies_ColorFromBv_m1935C8E628927002C86EC08773C04EB5AFC627D0 (double ___0_bv, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR double SkyBodies_DisplayGain_mBF76B46A7AC5A713D1F3DB9C294B22135B459073 (double ___0_vmag, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Mathf_Clamp01_mA7E048DBDA832D399A581BE4D6DED9FA44CE0F14_inline (float ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void StarBake_Stamp_m30ED1DF8B10E40D462BFE464F2A22E12F7E57416 (ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* ___0_px, int32_t ___1_w, int32_t ___2_h, int32_t ___3_cx, int32_t ___4_cy, Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 ___5_col, float ___6_gain, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Texture2D__ctor_mECF60A9EC0638EC353C02C8E99B6B465D23BE917 (Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* __this, int32_t ___0_width, int32_t ___1_height, int32_t ___2_textureFormat, bool ___3_mipChain, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Texture_set_wrapModeU_m2002CCB73143182032BD5ED6C80AB9A0ED2A0981 (Texture_t791CBB51219779964E0E8A2ED7C1AA5F92A4A700* __this, int32_t ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Texture_set_wrapModeV_m3C79177BAE70103DDBF871896AF464513A0F919D (Texture_t791CBB51219779964E0E8A2ED7C1AA5F92A4A700* __this, int32_t ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Texture_set_filterMode_mE423E58C0C16D059EA62BA87AD70F44AEA50CCC9 (Texture_t791CBB51219779964E0E8A2ED7C1AA5F92A4A700* __this, int32_t ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Texture_set_anisoLevel_m768759DE9D6BE15FCE995F5FC468980B904B9D1F (Texture_t791CBB51219779964E0E8A2ED7C1AA5F92A4A700* __this, int32_t ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Texture2D_SetPixels_mAE0CDFA15FA96F840D7FFADC31405D8AF20D9073 (Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* __this, ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* ___0_colors, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Texture2D_Apply_m36EE27E6F1BF7FB8C70A1D749DC4EE249810AA3A (Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* __this, bool ___0_updateMipmaps, bool ___1_makeNoLongerReadable, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_Normalize_m1DE89849FE2019B6AA0031306896F3B800849399_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 Quaternion_Internal_LookRotation_m489E6A5E916949FFE91C0B11A60C6016D996C1E0 (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* ___0_forward, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* ___1_upwards, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Vector3_get_magnitude_mF0D6017E90B345F1F52D1CC564C640F1A847AF2D_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* __this, const RuntimeMethod* method) ;
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
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float SkyRig_get_SunElevationDeg_mF44B2EEA5FE7E08C2D0329CF949CCA54E0065B8E (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkySun_t55412B2FF59478EDEEE01F79F3B0E738516396BD_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		il2cpp_codegen_runtime_class_init_inline(SkySun_t55412B2FF59478EDEEE01F79F3B0E738516396BD_il2cpp_TypeInfo_var);
		float L_0;
		L_0 = SkySun_get_ElevationDeg_m88C47C05DAB5E5D03500C5F8E53F677A31E17113_inline(NULL);
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_ForceTimeOfDay_mDE42607E9DE6AF0F25C108B0D689F08AB7D92B75 (double ___0_hoursLocal, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkySun_t55412B2FF59478EDEEE01F79F3B0E738516396BD_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* V_0 = NULL;
	double V_1 = 0.0;
	double V_2 = 0.0;
	int32_t V_3 = 0;
	int32_t V_4 = 0;
	int32_t V_5 = 0;
	double G_B3_0 = 0.0;
	double G_B6_0 = 0.0;
	int32_t G_B9_0 = 0;
	int32_t G_B12_0 = 0;
	int32_t G_B15_0 = 0;
	{
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->____forced = (bool)1;
		double L_0 = ___0_hoursLocal;
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->____forcedLocal = L_0;
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_1 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->____active;
		V_0 = L_1;
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_2 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_3;
		L_3 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_2, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (L_3)
		{
			goto IL_0026;
		}
	}
	{
		G_B3_0 = (28.5);
		goto IL_002c;
	}

IL_0026:
	{
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_4 = V_0;
		NullCheck(L_4);
		double L_5 = L_4->___LatitudeDeg;
		G_B3_0 = L_5;
	}

IL_002c:
	{
		V_1 = G_B3_0;
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_6 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_7;
		L_7 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_6, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (L_7)
		{
			goto IL_0041;
		}
	}
	{
		G_B6_0 = (-80.599999999999994);
		goto IL_0047;
	}

IL_0041:
	{
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_8 = V_0;
		NullCheck(L_8);
		double L_9 = L_8->___LongitudeDeg;
		G_B6_0 = L_9;
	}

IL_0047:
	{
		V_2 = G_B6_0;
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_10 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_11;
		L_11 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_10, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (L_11)
		{
			goto IL_0058;
		}
	}
	{
		G_B9_0 = ((int32_t)2026);
		goto IL_005e;
	}

IL_0058:
	{
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_12 = V_0;
		NullCheck(L_12);
		int32_t L_13 = L_12->___Year;
		G_B9_0 = L_13;
	}

IL_005e:
	{
		V_3 = G_B9_0;
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_14 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_15;
		L_15 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_14, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (L_15)
		{
			goto IL_006b;
		}
	}
	{
		G_B12_0 = 6;
		goto IL_0071;
	}

IL_006b:
	{
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_16 = V_0;
		NullCheck(L_16);
		int32_t L_17 = L_16->___Month;
		G_B12_0 = L_17;
	}

IL_0071:
	{
		V_4 = G_B12_0;
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_18 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_19;
		L_19 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_18, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (L_19)
		{
			goto IL_0080;
		}
	}
	{
		G_B15_0 = ((int32_t)21);
		goto IL_0086;
	}

IL_0080:
	{
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_20 = V_0;
		NullCheck(L_20);
		int32_t L_21 = L_20->___Day;
		G_B15_0 = L_21;
	}

IL_0086:
	{
		V_5 = G_B15_0;
		double L_22 = ___0_hoursLocal;
		double L_23 = V_1;
		double L_24 = V_2;
		int32_t L_25 = V_3;
		int32_t L_26 = V_4;
		int32_t L_27 = V_5;
		il2cpp_codegen_runtime_class_init_inline(SkySun_t55412B2FF59478EDEEE01F79F3B0E738516396BD_il2cpp_TypeInfo_var);
		SkySun_ApplyLocalHours_m9888C619611714B145E4637C51B646A7EF24396A(L_22, L_23, L_24, L_25, L_26, L_27, NULL);
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_28 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_29;
		L_29 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_28, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_29)
		{
			goto IL_009f;
		}
	}
	{
		return;
	}

IL_009f:
	{
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_30 = V_0;
		double L_31 = ___0_hoursLocal;
		double L_32 = V_2;
		double L_33;
		L_33 = SkyBodies_UtcFromLocalHours_m3ACF33FAD1AC595905E6A73B3E3E34649FA68244(L_31, L_32, NULL);
		NullCheck(L_30);
		L_30->___HoursUtc = L_33;
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_34 = V_0;
		NullCheck(L_34);
		L_34->____sunReady = (bool)0;
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_35 = V_0;
		NullCheck(L_35);
		L_35->____artReady = (bool)0;
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_36 = V_0;
		NullCheck(L_36);
		SkyRig_Refresh_mDBEAB6290ED6115E2BC879C0A77B1620DA523745(L_36, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C SkyRig_get_Grade_mB922C963C06C2B47481FCC64085AE25BA134C453 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) 
{
	{
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_0 = __this->___U3CGradeU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_set_Grade_m2DFB8271381DE1BAF1321B6B0E623E833031C8C0 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C ___0_value, const RuntimeMethod* method) 
{
	{
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_0 = ___0_value;
		__this->___U3CGradeU3Ek__BackingField = L_0;
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* SkyRig_Attach_m47461E471906514A6EB472C8EAC6E0EB20AD1E43 (Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* ___0_cam, Light_t1E68479B7782AF2050FAA02A5DC612FD034F18F3* ___1_sun, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Component_GetComponent_TisSkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_m73D462217630CF695C0E837C0C47D9677513B8FB_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&GameObject_AddComponent_TisSkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_m07A5EC1E86349D9E5C90BADD3D8452231C6C882C_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* V_0 = NULL;
	{
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_0 = ___0_cam;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_000b;
		}
	}
	{
		return (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0*)NULL;
	}

IL_000b:
	{
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_2 = ___0_cam;
		NullCheck(L_2);
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_3;
		L_3 = Component_GetComponent_TisSkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_m73D462217630CF695C0E837C0C47D9677513B8FB(L_2, Component_GetComponent_TisSkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_m73D462217630CF695C0E837C0C47D9677513B8FB_RuntimeMethod_var);
		V_0 = L_3;
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_4 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_5;
		L_5 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_4, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_5)
		{
			goto IL_0027;
		}
	}
	{
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_6 = ___0_cam;
		NullCheck(L_6);
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_7;
		L_7 = Component_get_gameObject_m57AEFBB14DB39EC476F740BA000E170355DE691B(L_6, NULL);
		NullCheck(L_7);
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_8;
		L_8 = GameObject_AddComponent_TisSkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_m07A5EC1E86349D9E5C90BADD3D8452231C6C882C(L_7, GameObject_AddComponent_TisSkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_m07A5EC1E86349D9E5C90BADD3D8452231C6C882C_RuntimeMethod_var);
		V_0 = L_8;
	}

IL_0027:
	{
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_9 = V_0;
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_10 = ___0_cam;
		NullCheck(L_9);
		L_9->___View = L_10;
		Il2CppCodeGenWriteBarrier((void**)(&L_9->___View), (void*)L_10);
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_11 = V_0;
		Light_t1E68479B7782AF2050FAA02A5DC612FD034F18F3* L_12 = ___1_sun;
		NullCheck(L_11);
		L_11->___SunLight = L_12;
		Il2CppCodeGenWriteBarrier((void**)(&L_11->___SunLight), (void*)L_12);
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_13 = V_0;
		NullCheck(L_13);
		SkyRig_PickTier_mFE1C3A8E6EC267D5DF49DC1A4DD784FB492D3F01(L_13, NULL);
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_14 = V_0;
		NullCheck(L_14);
		SkyRig_LoadWeather_m9CEFA38CC4D4407F5E670B05BE4B7CF47B35AF31(L_14, NULL);
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_15 = V_0;
		NullCheck(L_15);
		SkyRig_SuppressSkyPost_mC67F006FB6F2258DC110DC996760B15EF6360BC9(L_15, NULL);
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_16 = V_0;
		NullCheck(L_16);
		SkyRig_Refresh_mDBEAB6290ED6115E2BC879C0A77B1620DA523745(L_16, NULL);
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_17 = V_0;
		return L_17;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_OnEnable_m9D23FEC7987DE68B041822E2C2D2513BD26B5D12 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->____active = __this;
		Il2CppCodeGenWriteBarrier((void**)(&((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->____active), (void*)__this);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_OnDisable_mD24A668AEB8122444AFFE5C9A7666E1341C863AA (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* L_0 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->____active;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_0, __this, NULL);
		if (!L_1)
		{
			goto IL_0013;
		}
	}
	{
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->____active = (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0*)NULL;
		Il2CppCodeGenWriteBarrier((void**)(&((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->____active), (void*)(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0*)NULL);
	}

IL_0013:
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_SetAltitude_m14722CBC85CE3EBDE01D6765FD186FBC6931A2E1 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, double ___0_altitudeM, const RuntimeMethod* method) 
{
	{
		double L_0 = ___0_altitudeM;
		__this->____altitudeM = L_0;
		SkyRig_Refresh_mDBEAB6290ED6115E2BC879C0A77B1620DA523745(__this, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_SetWeather_m5F67192F2F9E0E3AE5FC2CA11C3635741781381C (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, WeatherDay_tBFA6624F16FBACC13014D8A250FFF8CD5AFA6EBC ___0_day, const RuntimeMethod* method) 
{
	{
		WeatherDay_tBFA6624F16FBACC13014D8A250FFF8CD5AFA6EBC L_0 = ___0_day;
		SkyWeather_t905CF34969B44F09A45D96F10FA58C6D01AE2DA2 L_1;
		L_1 = SkyWeather_From_mEC3BBC0876EC155ABA0C80CB9F4B2C637C190FB2(L_0, NULL);
		__this->___Weather = L_1;
		SkyRig_Refresh_mDBEAB6290ED6115E2BC879C0A77B1620DA523745(__this, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_SetTod_m2902A5A9404C72C93B4CF127869079A49FCC4D12 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, String_t* ___0_todId, const RuntimeMethod* method) 
{
	{
		String_t* L_0 = ___0_todId;
		__this->___TodId = L_0;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___TodId), (void*)L_0);
		__this->____artExplicit = (bool)1;
		__this->____artReady = (bool)0;
		SkyRig_Refresh_mDBEAB6290ED6115E2BC879C0A77B1620DA523745(__this, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_PickTier_mFE1C3A8E6EC267D5DF49DC1A4DD784FB492D3F01 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral4BD3052BB1EC7598F0952B3FA0F49095189128A3);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral598763C97CAEC44F4B760460963CD4730E60B870);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralFF22701255789DB31AE3E5235D2865849D0506EE);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	{
		int32_t L_0;
		L_0 = SystemInfo_get_systemMemorySize_m3BFE40CF5A43FEAB94F5C552A47D04ECD88B771E(NULL);
		V_0 = L_0;
		int32_t L_1 = V_0;
		if ((((int32_t)L_1) > ((int32_t)0)))
		{
			goto IL_0016;
		}
	}
	{
		__this->___Tier = _stringLiteral4BD3052BB1EC7598F0952B3FA0F49095189128A3;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___Tier), (void*)_stringLiteral4BD3052BB1EC7598F0952B3FA0F49095189128A3);
		return;
	}

IL_0016:
	{
		int32_t L_2 = V_0;
		if ((((int32_t)L_2) >= ((int32_t)((int32_t)3500))))
		{
			goto IL_002a;
		}
	}
	{
		__this->___Tier = _stringLiteral598763C97CAEC44F4B760460963CD4730E60B870;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___Tier), (void*)_stringLiteral598763C97CAEC44F4B760460963CD4730E60B870);
		return;
	}

IL_002a:
	{
		int32_t L_3 = V_0;
		if ((((int32_t)L_3) < ((int32_t)((int32_t)5500))))
		{
			goto IL_003e;
		}
	}
	{
		__this->___Tier = _stringLiteralFF22701255789DB31AE3E5235D2865849D0506EE;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___Tier), (void*)_stringLiteralFF22701255789DB31AE3E5235D2865849D0506EE);
		return;
	}

IL_003e:
	{
		__this->___Tier = _stringLiteral4BD3052BB1EC7598F0952B3FA0F49095189128A3;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___Tier), (void*)_stringLiteral4BD3052BB1EC7598F0952B3FA0F49095189128A3);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_LoadArt_m45B7895F5EE4253439D8857A0FDA283732A388F6 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) 
{
	String_t* V_0 = NULL;
	TodPalette_tAB1B213C4DCFC64D0D05FB909B64DCDFC89D5A5B* V_1 = NULL;
	Wash_t5331DFD3DC49F7853E64611FA43BB9DA9F585498* V_2 = NULL;
	SkyArt_t9BEFE797BE1852E9A478879F377E2884E3F7A751 V_3;
	memset((&V_3), 0, sizeof(V_3));
	il2cpp::utils::ExceptionSupportStack<RuntimeObject*, 1> __active_exceptions;
	VisualSpecData_tE23E510D926948561B6E3C7A1B4F65FC480434A5* G_B6_0 = NULL;
	VisualSpecData_tE23E510D926948561B6E3C7A1B4F65FC480434A5* G_B5_0 = NULL;
	VisualSpecData_tE23E510D926948561B6E3C7A1B4F65FC480434A5* G_B7_0 = NULL;
	String_t* G_B8_0 = NULL;
	VisualSpecData_tE23E510D926948561B6E3C7A1B4F65FC480434A5* G_B8_1 = NULL;
	{
		String_t* L_0 = __this->___TodId;
		bool L_1;
		L_1 = String_IsNullOrEmpty_mEA9E3FB005AC28FE02E69FCF95A7B8456192B478(L_0, NULL);
		if (!L_1)
		{
			goto IL_001c;
		}
	}
	{
		bool L_2 = __this->____artExplicit;
		if (L_2)
		{
			goto IL_001c;
		}
	}
	{
		__this->___TodId = (String_t*)NULL;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___TodId), (void*)(String_t*)NULL);
	}

IL_001c:
	{
	}
	try
	{
		{
			VisualSpecData_tE23E510D926948561B6E3C7A1B4F65FC480434A5* L_3;
			L_3 = VisualSpecData_Load_mC791D3DC1CE4B9E4F82A350C91F6CFA274D34FD0(NULL);
			bool L_4 = __this->____artExplicit;
			if (!L_4)
			{
				G_B6_0 = L_3;
				goto IL_0037_1;
			}
			G_B5_0 = L_3;
		}
		{
			String_t* L_5 = __this->___TodId;
			bool L_6;
			L_6 = String_IsNullOrEmpty_mEA9E3FB005AC28FE02E69FCF95A7B8456192B478(L_5, NULL);
			if (!L_6)
			{
				G_B7_0 = G_B5_0;
				goto IL_003f_1;
			}
			G_B6_0 = G_B5_0;
		}

IL_0037_1:
		{
			String_t* L_7;
			L_7 = SkyRig_TodForSun_mB83FE28562D7B50BFEA0268AF1D05326DA333030(__this, NULL);
			G_B8_0 = L_7;
			G_B8_1 = G_B6_0;
			goto IL_0045_1;
		}

IL_003f_1:
		{
			String_t* L_8 = __this->___TodId;
			G_B8_0 = L_8;
			G_B8_1 = G_B7_0;
		}

IL_0045_1:
		{
			V_0 = G_B8_0;
			String_t* L_9 = V_0;
			NullCheck(G_B8_1);
			TodPalette_tAB1B213C4DCFC64D0D05FB909B64DCDFC89D5A5B* L_10;
			L_10 = VisualSpecData_Palette_mA17CAA995B096EC32C554DB8333D97E20338E8BF(G_B8_1, L_9, NULL);
			V_1 = L_10;
			TodPalette_tAB1B213C4DCFC64D0D05FB909B64DCDFC89D5A5B* L_11 = V_1;
			NullCheck(L_11);
			Wash_t5331DFD3DC49F7853E64611FA43BB9DA9F585498* L_12 = L_11->___wash;
			V_2 = L_12;
			il2cpp_codegen_initobj((&V_3), sizeof(SkyArt_t9BEFE797BE1852E9A478879F377E2884E3F7A751));
			Wash_t5331DFD3DC49F7853E64611FA43BB9DA9F585498* L_13 = V_2;
			NullCheck(L_13);
			String_t* L_14 = L_13->___horizon;
			Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_15;
			L_15 = Rgb_FromHex_m5B4493AE3CE25CE85C2247E98D1BBB2AA90D7B7A(L_14, NULL);
			(&V_3)->___Horizon = L_15;
			Wash_t5331DFD3DC49F7853E64611FA43BB9DA9F585498* L_16 = V_2;
			NullCheck(L_16);
			String_t* L_17 = L_16->___near;
			Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_18;
			L_18 = Rgb_FromHex_m5B4493AE3CE25CE85C2247E98D1BBB2AA90D7B7A(L_17, NULL);
			(&V_3)->___Near = L_18;
			Wash_t5331DFD3DC49F7853E64611FA43BB9DA9F585498* L_19 = V_2;
			NullCheck(L_19);
			String_t* L_20 = L_19->___space;
			Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_21;
			L_21 = Rgb_FromHex_m5B4493AE3CE25CE85C2247E98D1BBB2AA90D7B7A(L_20, NULL);
			(&V_3)->___Space = L_21;
			TodPalette_tAB1B213C4DCFC64D0D05FB909B64DCDFC89D5A5B* L_22 = V_1;
			NullCheck(L_22);
			float L_23 = L_22->___stars;
			(&V_3)->___StarFloor = ((double)L_23);
			SkyArt_t9BEFE797BE1852E9A478879F377E2884E3F7A751 L_24 = V_3;
			__this->___Art = L_24;
			String_t* L_25 = V_0;
			__this->___TodId = L_25;
			Il2CppCodeGenWriteBarrier((void**)(&__this->___TodId), (void*)L_25);
			goto IL_00ef;
		}
	}
	catch(Il2CppExceptionWrapper& e)
	{
		if(il2cpp_codegen_class_is_assignable_from (((RuntimeClass*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&Exception_t_il2cpp_TypeInfo_var)), il2cpp_codegen_object_class(e.ex)))
		{
			IL2CPP_PUSH_ACTIVE_EXCEPTION(e.ex);
			goto CATCH_00b0;
		}
		throw e;
	}

CATCH_00b0:
	{
		{
			Exception_t* L_26 = ((Exception_t*)IL2CPP_GET_ACTIVE_EXCEPTION(Exception_t*));;
			SkyArt_t9BEFE797BE1852E9A478879F377E2884E3F7A751* L_27 = (SkyArt_t9BEFE797BE1852E9A478879F377E2884E3F7A751*)(&__this->___Art);
			double L_28 = L_27->___StarFloor;
			if ((!(((double)L_28) <= ((double)(0.0)))))
			{
				goto IL_00ed;
			}
		}
		{
			SkyArt_t9BEFE797BE1852E9A478879F377E2884E3F7A751* L_29 = (SkyArt_t9BEFE797BE1852E9A478879F377E2884E3F7A751*)(&__this->___Art);
			Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398* L_30 = (Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398*)(&L_29->___Horizon);
			double L_31 = L_30->___B;
			if ((!(((double)L_31) <= ((double)(0.0)))))
			{
				goto IL_00ed;
			}
		}
		{
			SkyArt_t9BEFE797BE1852E9A478879F377E2884E3F7A751 L_32;
			L_32 = SkyArt_get_Neutral_m41D00FAAB2812B17D162EEA12A406303A987E85E(NULL);
			__this->___Art = L_32;
		}

IL_00ed:
		{
			IL2CPP_POP_ACTIVE_EXCEPTION(Exception_t*);
			goto IL_00ef;
		}
	}

IL_00ef:
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* SkyRig_TodForSun_mB83FE28562D7B50BFEA0268AF1D05326DA333030 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral2E15C8A32B8D7E0A09FD8A1C5B39D9A2A2BD499A);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral379BC7677CE810FF72085C7A382C70B00640541D);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral516C5BEFDA03D6A9E669102B2FE72C5AD0CD6E56);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral7CA8FDA9E34615850FDCF919A6FEC0033FC753A4);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral8298264B55E8E831493E62497185C522B30F5D88);
		s_Il2CppMethodInitialized = true;
	}
	double V_0 = 0.0;
	{
		double L_0 = __this->___LatitudeDeg;
		double L_1 = __this->___LongitudeDeg;
		int32_t L_2 = __this->___Year;
		int32_t L_3 = __this->___Month;
		int32_t L_4 = __this->___Day;
		double L_5 = __this->___HoursUtc;
		double L_6;
		L_6 = SkyBodies_SunElevationRad_mF217103BA291B956A97948403542A179DA55F6E4(L_0, L_1, L_2, L_3, L_4, L_5, NULL);
		V_0 = ((double)il2cpp_codegen_multiply(L_6, (57.295779513082323)));
		double L_7 = V_0;
		if ((!(((double)L_7) < ((double)(-4.0)))))
		{
			goto IL_0046;
		}
	}
	{
		return _stringLiteral8298264B55E8E831493E62497185C522B30F5D88;
	}

IL_0046:
	{
		double L_8 = V_0;
		if ((!(((double)L_8) < ((double)(6.0)))))
		{
			goto IL_0058;
		}
	}
	{
		return _stringLiteral516C5BEFDA03D6A9E669102B2FE72C5AD0CD6E56;
	}

IL_0058:
	{
		double L_9 = V_0;
		if ((!(((double)L_9) < ((double)(16.0)))))
		{
			goto IL_006a;
		}
	}
	{
		return _stringLiteral379BC7677CE810FF72085C7A382C70B00640541D;
	}

IL_006a:
	{
		double L_10 = V_0;
		if ((!(((double)L_10) < ((double)(28.0)))))
		{
			goto IL_008d;
		}
	}
	{
		double L_11 = __this->___HoursUtc;
		if ((!(((double)L_11) < ((double)(15.0)))))
		{
			goto IL_008d;
		}
	}
	{
		return _stringLiteral2E15C8A32B8D7E0A09FD8A1C5B39D9A2A2BD499A;
	}

IL_008d:
	{
		return _stringLiteral7CA8FDA9E34615850FDCF919A6FEC0033FC753A4;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_LoadWeather_m9CEFA38CC4D4407F5E670B05BE4B7CF47B35AF31 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral14A85563EBD186910C075E2F651ADF038BCE4507);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralA897E8AE058B1C0A5807FF9F03EB98A76BEAC8D2);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	il2cpp::utils::ExceptionSupportStack<RuntimeObject*, 1> __active_exceptions;
	try
	{
		int32_t L_0 = __this->___Year;
		int32_t L_1 = __this->___Month;
		int32_t L_2 = __this->___Day;
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		int32_t L_3;
		L_3 = SkyRig_DayOfYear_mDAC0DB5A12D5B64FB1909622D6313A6682216BA8(L_0, L_1, L_2, NULL);
		V_0 = L_3;
		int32_t L_4 = V_0;
		WeatherDay_tBFA6624F16FBACC13014D8A250FFF8CD5AFA6EBC L_5;
		L_5 = WeatherGenerator_Generate_m81762F3EB04ECFCB02FC7CEA5DD7A3DC0EE70BA5(_stringLiteralA897E8AE058B1C0A5807FF9F03EB98A76BEAC8D2, L_4, 1, _stringLiteral14A85563EBD186910C075E2F651ADF038BCE4507, NULL);
		SkyWeather_t905CF34969B44F09A45D96F10FA58C6D01AE2DA2 L_6;
		L_6 = SkyWeather_From_mEC3BBC0876EC155ABA0C80CB9F4B2C637C190FB2(L_5, NULL);
		__this->___Weather = L_6;
		goto IL_0044;
	}
	catch(Il2CppExceptionWrapper& e)
	{
		if(il2cpp_codegen_class_is_assignable_from (((RuntimeClass*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&Exception_t_il2cpp_TypeInfo_var)), il2cpp_codegen_object_class(e.ex)))
		{
			IL2CPP_PUSH_ACTIVE_EXCEPTION(e.ex);
			goto CATCH_0036;
		}
		throw e;
	}

CATCH_0036:
	{
		Exception_t* L_7 = ((Exception_t*)IL2CPP_GET_ACTIVE_EXCEPTION(Exception_t*));;
		SkyWeather_t905CF34969B44F09A45D96F10FA58C6D01AE2DA2 L_8;
		L_8 = SkyWeather_get_Clear_m0951F86F60733CA40966791E0BF16A102EF60774(NULL);
		__this->___Weather = L_8;
		IL2CPP_POP_ACTIVE_EXCEPTION(Exception_t*);
		goto IL_0044;
	}

IL_0044:
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t SkyRig_DayOfYear_mDAC0DB5A12D5B64FB1909622D6313A6682216BA8 (int32_t ___0_year, int32_t ___1_month, int32_t ___2_day, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&U3CPrivateImplementationDetailsU3E_t73601B3260B603D2A168CBC6A785CD2B6F85B460____3F1431B31172B4BE5418BDA2F4063F512F2F7A7CFDCE81A60B131792B5F7F694_FieldInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	int32_t V_1 = 0;
	Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* G_B4_0 = NULL;
	Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* G_B1_0 = NULL;
	Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* G_B3_0 = NULL;
	Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* G_B2_0 = NULL;
	int32_t G_B5_0 = 0;
	Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* G_B5_1 = NULL;
	int32_t G_B9_0 = 0;
	{
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_0 = (Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C*)(Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C*)SZArrayNew(Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C_il2cpp_TypeInfo_var, (uint32_t)((int32_t)12));
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_1 = L_0;
		RuntimeFieldHandle_t6E4C45B6D2EA12FC99185805A7E77527899B25C5 L_2 = { reinterpret_cast<intptr_t> (U3CPrivateImplementationDetailsU3E_t73601B3260B603D2A168CBC6A785CD2B6F85B460____3F1431B31172B4BE5418BDA2F4063F512F2F7A7CFDCE81A60B131792B5F7F694_FieldInfo_var) };
		RuntimeHelpers_InitializeArray_m751372AA3F24FBF6DA9B9D687CBFA2DE436CAB9B((RuntimeArray*)L_1, L_2, NULL);
		int32_t L_3 = ___1_month;
		if ((((int32_t)L_3) < ((int32_t)1)))
		{
			G_B4_0 = L_1;
			goto IL_0022;
		}
		G_B1_0 = L_1;
	}
	{
		int32_t L_4 = ___1_month;
		if ((((int32_t)L_4) > ((int32_t)((int32_t)12))))
		{
			G_B3_0 = G_B1_0;
			goto IL_001e;
		}
		G_B2_0 = G_B1_0;
	}
	{
		int32_t L_5 = ___1_month;
		G_B5_0 = L_5;
		G_B5_1 = G_B2_0;
		goto IL_0023;
	}

IL_001e:
	{
		G_B5_0 = ((int32_t)12);
		G_B5_1 = G_B3_0;
		goto IL_0023;
	}

IL_0022:
	{
		G_B5_0 = 1;
		G_B5_1 = G_B4_0;
	}

IL_0023:
	{
		V_0 = G_B5_0;
		int32_t L_6 = V_0;
		NullCheck(G_B5_1);
		int32_t L_7 = ((int32_t)il2cpp_codegen_subtract(L_6, 1));
		int32_t L_8 = (G_B5_1)->GetAt(static_cast<il2cpp_array_size_t>(L_7));
		int32_t L_9 = ___2_day;
		V_1 = ((int32_t)il2cpp_codegen_add(L_8, L_9));
		int32_t L_10 = ___0_year;
		if (((int32_t)(L_10%4)))
		{
			goto IL_0036;
		}
	}
	{
		int32_t L_11 = ___0_year;
		if (((int32_t)(L_11%((int32_t)100))))
		{
			goto IL_0042;
		}
	}

IL_0036:
	{
		int32_t L_12 = ___0_year;
		G_B9_0 = ((((int32_t)((int32_t)(L_12%((int32_t)400)))) == ((int32_t)0))? 1 : 0);
		goto IL_0043;
	}

IL_0042:
	{
		G_B9_0 = 1;
	}

IL_0043:
	{
		if (!G_B9_0)
		{
			goto IL_004d;
		}
	}
	{
		int32_t L_13 = V_0;
		if ((((int32_t)L_13) <= ((int32_t)2)))
		{
			goto IL_004d;
		}
	}
	{
		int32_t L_14 = V_1;
		V_1 = ((int32_t)il2cpp_codegen_add(L_14, 1));
	}

IL_004d:
	{
		int32_t L_15 = V_1;
		if ((((int32_t)L_15) >= ((int32_t)1)))
		{
			goto IL_0053;
		}
	}
	{
		V_1 = 1;
	}

IL_0053:
	{
		int32_t L_16 = V_1;
		if ((((int32_t)L_16) <= ((int32_t)((int32_t)365))))
		{
			goto IL_0061;
		}
	}
	{
		V_1 = ((int32_t)365);
	}

IL_0061:
	{
		int32_t L_17 = V_1;
		return L_17;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_Refresh_mDBEAB6290ED6115E2BC879C0A77B1620DA523745 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkySun_t55412B2FF59478EDEEE01F79F3B0E738516396BD_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		SkyRig_ApplyCommandLine_mBF59C29ACCB257D543549FF7F4A73532BE4A34BD(NULL);
		SkyRig_EnsureMaterial_m8C085242757B7A62E4ACE2DC2D1C1483DCC742B7(__this, NULL);
		SkyRig_SuppressSkyPost_mC67F006FB6F2258DC110DC996760B15EF6360BC9(__this, NULL);
		bool L_0;
		L_0 = SkyRig_SampleSun_m308A2A28229172EF98B04ADB14986A1AD2C2B949(__this, NULL);
		double L_1 = __this->____elev;
		il2cpp_codegen_runtime_class_init_inline(SkySun_t55412B2FF59478EDEEE01F79F3B0E738516396BD_il2cpp_TypeInfo_var);
		SkySun_Publish_m0C85B44B4C354730B45B7E66EE056A1FB48B795F(L_1, NULL);
		if (!L_0)
		{
			goto IL_002b;
		}
	}
	{
		__this->____artReady = (bool)0;
	}

IL_002b:
	{
		bool L_2 = __this->____artReady;
		if (L_2)
		{
			goto IL_0040;
		}
	}
	{
		SkyRig_LoadArt_m45B7895F5EE4253439D8857A0FDA283732A388F6(__this, NULL);
		__this->____artReady = (bool)1;
	}

IL_0040:
	{
		double L_3 = __this->____altitudeM;
		double L_4 = __this->____elev;
		SkyArt_t9BEFE797BE1852E9A478879F377E2884E3F7A751 L_5 = __this->___Art;
		SkyWeather_t905CF34969B44F09A45D96F10FA58C6D01AE2DA2 L_6 = __this->___Weather;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_7;
		L_7 = SkyColor_At_m4282570A7DDEC1791A2429368F1133D5C23B4127(L_3, L_4, L_5, L_6, (String_t*)NULL, NULL);
		SkyRig_set_Grade_m2DFB8271381DE1BAF1321B6B0E623E833031C8C0_inline(__this, L_7, NULL);
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_8;
		L_8 = SkyRig_get_Grade_mB922C963C06C2B47481FCC64085AE25BA134C453_inline(__this, NULL);
		SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D L_9 = __this->____sunDir;
		SkyRig_Apply_m335778822D8B098D9CC60D14674773698488EAAD(__this, L_8, L_9, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_ApplyCommandLine_mBF59C29ACCB257D543549FF7F4A73532BE4A34BD (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		bool L_0 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->____cliChecked;
		if (!L_0)
		{
			goto IL_0008;
		}
	}
	{
		return;
	}

IL_0008:
	{
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->____cliChecked = (bool)1;
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_1;
		L_1 = Environment_GetCommandLineArgs_mD29CFA1CD3C84F9BD91152E70302E908114A831D(NULL);
		bool L_2;
		L_2 = SkyBodies_HasNightArg_mDC38C1F5DD3FDECC5C4928838C5350B3D2014DF9(L_1, NULL);
		if (!L_2)
		{
			goto IL_0028;
		}
	}
	{
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		SkyRig_ForceTimeOfDay_mDE42607E9DE6AF0F25C108B0D689F08AB7D92B75((2.0), NULL);
	}

IL_0028:
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool SkyRig_SampleSun_m308A2A28229172EF98B04ADB14986A1AD2C2B949 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	double V_0 = 0.0;
	{
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		bool L_0 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->____forced;
		if (!L_0)
		{
			goto IL_001d;
		}
	}
	{
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		double L_1 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->____forcedLocal;
		double L_2 = __this->___LongitudeDeg;
		double L_3;
		L_3 = SkyBodies_UtcFromLocalHours_m3ACF33FAD1AC595905E6A73B3E3E34649FA68244(L_1, L_2, NULL);
		__this->___HoursUtc = L_3;
	}

IL_001d:
	{
		double L_4 = __this->___HoursUtc;
		int32_t L_5 = __this->___Day;
		int32_t L_6 = __this->___Month;
		int32_t L_7 = __this->___Year;
		double L_8 = __this->___LatitudeDeg;
		double L_9 = __this->___LongitudeDeg;
		V_0 = ((double)il2cpp_codegen_add(((double)il2cpp_codegen_add(((double)il2cpp_codegen_add(((double)il2cpp_codegen_add(((double)il2cpp_codegen_add(L_4, ((double)il2cpp_codegen_multiply(((double)L_5), (48.0))))), ((double)il2cpp_codegen_multiply(((double)L_6), (2000.0))))), ((double)il2cpp_codegen_multiply(((double)L_7), (40000.0))))), ((double)il2cpp_codegen_multiply(L_8, (0.01))))), ((double)il2cpp_codegen_multiply(L_9, (0.0001)))));
		bool L_10 = __this->____sunReady;
		if (!L_10)
		{
			goto IL_008f;
		}
	}
	{
		double L_11 = V_0;
		double L_12 = __this->____clockKey;
		if ((!(((double)L_11) == ((double)L_12))))
		{
			goto IL_008f;
		}
	}
	{
		return (bool)0;
	}

IL_008f:
	{
		double L_13 = V_0;
		__this->____clockKey = L_13;
		double L_14 = __this->___LatitudeDeg;
		double L_15 = __this->___LongitudeDeg;
		int32_t L_16 = __this->___Year;
		int32_t L_17 = __this->___Month;
		int32_t L_18 = __this->___Day;
		double L_19 = __this->___HoursUtc;
		double L_20;
		L_20 = SkyBodies_SunElevationRad_mF217103BA291B956A97948403542A179DA55F6E4(L_14, L_15, L_16, L_17, L_18, L_19, NULL);
		__this->____elev = L_20;
		double L_21 = __this->___LatitudeDeg;
		double L_22 = __this->___LongitudeDeg;
		int32_t L_23 = __this->___Year;
		int32_t L_24 = __this->___Month;
		int32_t L_25 = __this->___Day;
		double L_26 = __this->___HoursUtc;
		SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D L_27;
		L_27 = SkyBodies_SunUnity_m208FFDA36A2CD87C7FF8D15FE87D67077F03D2D6(L_21, L_22, L_23, L_24, L_25, L_26, NULL);
		__this->____sunDir = L_27;
		__this->____sunReady = (bool)1;
		return (bool)1;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_SuppressSkyPost_mC67F006FB6F2258DC110DC996760B15EF6360BC9 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkyRig_SuppressSkyPost_mC67F006FB6F2258DC110DC996760B15EF6360BC9_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Type_GetType_m71A077E0B5DA3BD1DC0AB9AE387056CFCF56F93F_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral57B5D0F93C2D506E618E23187302C0FADE813B93);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralA2D7FD624675BA3D65DD239C532166DF399D7A7E);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralB1957EC98586D602AFDF307B9597C5FFD6178D65);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralED73DBABE38E94896AF0E57AD10F86530B11BBA4);
		s_Il2CppMethodInitialized = true;
	}
	Type_t* V_0 = NULL;
	Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* V_1 = NULL;
	PropertyInfo_t* V_2 = NULL;
	{
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_0 = __this->___View;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_000f;
		}
	}
	{
		return;
	}

IL_000f:
	{
		il2cpp_codegen_runtime_class_init_inline(il2cpp_defaults.systemtype_class);
		Type_t* L_2;
		L_2 = il2cpp_codegen_get_type(_stringLiteral57B5D0F93C2D506E618E23187302C0FADE813B93, Type_GetType_m71A077E0B5DA3BD1DC0AB9AE387056CFCF56F93F_RuntimeMethod_var, SkyRig_SuppressSkyPost_mC67F006FB6F2258DC110DC996760B15EF6360BC9_RuntimeMethod_var);
		V_0 = L_2;
		Type_t* L_3 = V_0;
		bool L_4;
		L_4 = Type_op_Equality_m99930A0E44E420A685FABA60E60BA1CC5FA0EBDC(L_3, (Type_t*)NULL, NULL);
		if (!L_4)
		{
			goto IL_0024;
		}
	}
	{
		return;
	}

IL_0024:
	{
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_5 = __this->___View;
		Type_t* L_6 = V_0;
		NullCheck(L_5);
		Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* L_7;
		L_7 = Component_GetComponent_m4352437B839B9601EB007CAC1007C8E1FB8A427D(L_5, L_6, NULL);
		V_1 = L_7;
		Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* L_8 = V_1;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_9;
		L_9 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_8, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_9)
		{
			goto IL_004c;
		}
	}
	{
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_10 = __this->___View;
		NullCheck(L_10);
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_11;
		L_11 = Component_get_gameObject_m57AEFBB14DB39EC476F740BA000E170355DE691B(L_10, NULL);
		Type_t* L_12 = V_0;
		NullCheck(L_11);
		Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* L_13;
		L_13 = GameObject_AddComponent_mDF246771EC34613FA6AF0C98D443368FB43E9F36(L_11, L_12, NULL);
		V_1 = L_13;
	}

IL_004c:
	{
		Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* L_14 = V_1;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_15;
		L_15 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_14, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_15)
		{
			goto IL_0056;
		}
	}
	{
		return;
	}

IL_0056:
	{
		Type_t* L_16 = V_0;
		Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* L_17 = V_1;
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		SkyRig_SetMember_m9361B75A5116B5F4FAF6AC020CD306FFCE63E947(L_16, L_17, _stringLiteralB1957EC98586D602AFDF307B9597C5FFD6178D65, (bool)0, NULL);
		Type_t* L_18 = V_0;
		Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* L_19 = V_1;
		SkyRig_SetMember_m9361B75A5116B5F4FAF6AC020CD306FFCE63E947(L_18, L_19, _stringLiteralED73DBABE38E94896AF0E57AD10F86530B11BBA4, (bool)0, NULL);
		Type_t* L_20 = V_0;
		NullCheck(L_20);
		PropertyInfo_t* L_21;
		L_21 = Type_GetProperty_mD183124FC8A89121E8368058B327A7750B14281D(L_20, _stringLiteralA2D7FD624675BA3D65DD239C532166DF399D7A7E, NULL);
		V_2 = L_21;
		PropertyInfo_t* L_22 = V_2;
		bool L_23;
		L_23 = PropertyInfo_op_Inequality_mE75A4F14CC678D8A670730FBD4338C718CACB51B(L_22, (PropertyInfo_t*)NULL, NULL);
		if (!L_23)
		{
			goto IL_00ae;
		}
	}
	{
		PropertyInfo_t* L_24 = V_2;
		NullCheck(L_24);
		bool L_25;
		L_25 = VirtualFuncInvoker0< bool >::Invoke(17, L_24);
		if (!L_25)
		{
			goto IL_00ae;
		}
	}
	{
		PropertyInfo_t* L_26 = V_2;
		NullCheck(L_26);
		Type_t* L_27;
		L_27 = VirtualFuncInvoker0< Type_t* >::Invoke(15, L_26);
		NullCheck(L_27);
		bool L_28;
		L_28 = VirtualFuncInvoker0< bool >::Invoke(68, L_27);
		if (!L_28)
		{
			goto IL_00ae;
		}
	}
	{
		PropertyInfo_t* L_29 = V_2;
		Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* L_30 = V_1;
		PropertyInfo_t* L_31 = V_2;
		NullCheck(L_31);
		Type_t* L_32;
		L_32 = VirtualFuncInvoker0< Type_t* >::Invoke(15, L_31);
		il2cpp_codegen_runtime_class_init_inline(il2cpp_defaults.enum_class);
		RuntimeObject* L_33;
		L_33 = Enum_ToObject_m7952B7BD43E48BBF79BB973BD31E7FF9E62B948E(L_32, 0, NULL);
		NullCheck(L_29);
		VirtualActionInvoker3< RuntimeObject*, RuntimeObject*, ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918* >::Invoke(23, L_29, L_30, L_33, (ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918*)NULL);
	}

IL_00ae:
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_SetMember_m9361B75A5116B5F4FAF6AC020CD306FFCE63E947 (Type_t* ___0_t, RuntimeObject* ___1_target, String_t* ___2_name, bool ___3_value, const RuntimeMethod* method) 
{
	PropertyInfo_t* V_0 = NULL;
	{
		Type_t* L_0 = ___0_t;
		String_t* L_1 = ___2_name;
		NullCheck(L_0);
		PropertyInfo_t* L_2;
		L_2 = Type_GetProperty_mD183124FC8A89121E8368058B327A7750B14281D(L_0, L_1, NULL);
		V_0 = L_2;
		PropertyInfo_t* L_3 = V_0;
		bool L_4;
		L_4 = PropertyInfo_op_Inequality_mE75A4F14CC678D8A670730FBD4338C718CACB51B(L_3, (PropertyInfo_t*)NULL, NULL);
		if (!L_4)
		{
			goto IL_003e;
		}
	}
	{
		PropertyInfo_t* L_5 = V_0;
		NullCheck(L_5);
		bool L_6;
		L_6 = VirtualFuncInvoker0< bool >::Invoke(17, L_5);
		if (!L_6)
		{
			goto IL_003e;
		}
	}
	{
		PropertyInfo_t* L_7 = V_0;
		NullCheck(L_7);
		Type_t* L_8;
		L_8 = VirtualFuncInvoker0< Type_t* >::Invoke(15, L_7);
		RuntimeTypeHandle_t332A452B8B6179E4469B69525D0FE82A88030F7B L_9 = { reinterpret_cast<intptr_t> (&il2cpp_defaults.boolean_class->byval_arg) };
		il2cpp_codegen_runtime_class_init_inline(il2cpp_defaults.systemtype_class);
		Type_t* L_10;
		L_10 = Type_GetTypeFromHandle_m6062B81682F79A4D6DF2640692EE6D9987858C57(L_9, NULL);
		bool L_11;
		L_11 = Type_op_Equality_m99930A0E44E420A685FABA60E60BA1CC5FA0EBDC(L_8, L_10, NULL);
		if (!L_11)
		{
			goto IL_003e;
		}
	}
	{
		PropertyInfo_t* L_12 = V_0;
		RuntimeObject* L_13 = ___1_target;
		bool L_14 = ___3_value;
		bool L_15 = L_14;
		RuntimeObject* L_16 = Box(il2cpp_defaults.boolean_class, &L_15);
		NullCheck(L_12);
		VirtualActionInvoker3< RuntimeObject*, RuntimeObject*, ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918* >::Invoke(23, L_12, L_13, L_16, (ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918*)NULL);
	}

IL_003e:
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_EnsureMaterial_m8C085242757B7A62E4ACE2DC2D1C1483DCC742B7 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Debug_t8394C7EEAECA3689C2C9B9DE9C7166D73596276F_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Resources_Load_TisShader_tADC867D36B7876EE22427FAA2CE485105F4EE692_m378804064185FA25C41237187B3CCEA095C05946_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral0DB39025454CDDC2BE10F219994530C7C0F0FEE8);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral362D46D698B33ACEE2C21206D012143EA884291E);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral787389110EAF5516339BFA7A1DB299BF18596896);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralBB7588CFAD6537BAD69E10A3C337B7FF5790D51A);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralD7AD6BB90087503B8D40592A56FDBB40E5627C3A);
		s_Il2CppMethodInitialized = true;
	}
	Shader_tADC867D36B7876EE22427FAA2CE485105F4EE692* V_0 = NULL;
	{
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_0 = __this->____mat;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_000f;
		}
	}
	{
		return;
	}

IL_000f:
	{
		Shader_tADC867D36B7876EE22427FAA2CE485105F4EE692* L_2;
		L_2 = Resources_Load_TisShader_tADC867D36B7876EE22427FAA2CE485105F4EE692_m378804064185FA25C41237187B3CCEA095C05946(_stringLiteral787389110EAF5516339BFA7A1DB299BF18596896, Resources_Load_TisShader_tADC867D36B7876EE22427FAA2CE485105F4EE692_m378804064185FA25C41237187B3CCEA095C05946_RuntimeMethod_var);
		V_0 = L_2;
		Shader_tADC867D36B7876EE22427FAA2CE485105F4EE692* L_3 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_4;
		L_4 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_3, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_4)
		{
			goto IL_002e;
		}
	}
	{
		Shader_tADC867D36B7876EE22427FAA2CE485105F4EE692* L_5;
		L_5 = Shader_Find_m183AA54F78320212DDEC811592F98456898A41C5(_stringLiteral362D46D698B33ACEE2C21206D012143EA884291E, NULL);
		V_0 = L_5;
	}

IL_002e:
	{
		Shader_tADC867D36B7876EE22427FAA2CE485105F4EE692* L_6 = V_0;
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		bool L_7;
		L_7 = SkyRig_IsErrorShader_mC4CDA81F46C7A7F50E9979F84005B73B429AF45A(L_6, NULL);
		if (!L_7)
		{
			goto IL_0041;
		}
	}
	{
		il2cpp_codegen_runtime_class_init_inline(Debug_t8394C7EEAECA3689C2C9B9DE9C7166D73596276F_il2cpp_TypeInfo_var);
		Debug_LogError_mB00B2B4468EF3CAF041B038D840820FB84C924B2(_stringLiteralD7AD6BB90087503B8D40592A56FDBB40E5627C3A, NULL);
		return;
	}

IL_0041:
	{
		Shader_tADC867D36B7876EE22427FAA2CE485105F4EE692* L_8 = V_0;
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_9 = (Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3*)il2cpp_codegen_object_new(Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3_il2cpp_TypeInfo_var);
		Material__ctor_m7FDF47105D66D19591BE505A0C42B0F90D88C9BF(L_9, L_8, NULL);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_10 = L_9;
		NullCheck(L_10);
		Object_set_name_mC79E6DC8FFD72479C90F0C4CC7F42A0FEAF5AE47(L_10, _stringLiteralBB7588CFAD6537BAD69E10A3C337B7FF5790D51A, NULL);
		__this->____mat = L_10;
		Il2CppCodeGenWriteBarrier((void**)(&__this->____mat), (void*)L_10);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_11 = __this->____mat;
		NullCheck(L_11);
		Shader_tADC867D36B7876EE22427FAA2CE485105F4EE692* L_12;
		L_12 = Material_get_shader_m8B0C11AE6F2AD7DE30AF52D3195EB716F7A71983(L_11, NULL);
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		bool L_13;
		L_13 = SkyRig_IsErrorShader_mC4CDA81F46C7A7F50E9979F84005B73B429AF45A(L_12, NULL);
		if (!L_13)
		{
			goto IL_0087;
		}
	}
	{
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_14 = __this->____mat;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		Object_Destroy_mE97D0A766419A81296E8D4E5C23D01D3FE91ACBB(L_14, NULL);
		__this->____mat = (Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3*)NULL;
		Il2CppCodeGenWriteBarrier((void**)(&__this->____mat), (void*)(Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3*)NULL);
		il2cpp_codegen_runtime_class_init_inline(Debug_t8394C7EEAECA3689C2C9B9DE9C7166D73596276F_il2cpp_TypeInfo_var);
		Debug_LogError_mB00B2B4468EF3CAF041B038D840820FB84C924B2(_stringLiteral0DB39025454CDDC2BE10F219994530C7C0F0FEE8, NULL);
		return;
	}

IL_0087:
	{
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_15 = __this->____mat;
		NullCheck(L_15);
		Object_set_hideFlags_mACB8BFC903FB3B01BBD427753E791BF28B5E33D4(L_15, ((int32_t)61), NULL);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_16 = __this->____mat;
		RenderSettings_set_skybox_m7988CC3217B1456CA0A7294127527BFC18EEC872(L_16, NULL);
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_17 = __this->___View;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_18;
		L_18 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_17, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_18)
		{
			goto IL_00b9;
		}
	}
	{
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_19 = __this->___View;
		NullCheck(L_19);
		Camera_set_clearFlags_m66541D9CC43CBAA5FE7364A50D43CA5569FD4D93(L_19, 1, NULL);
	}

IL_00b9:
	{
		SkyRig_BakeStars_m7E771561D418AD46D63615396CBE9FBF52240743(__this, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_BakeStars_m7E771561D418AD46D63615396CBE9FBF52240743 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_RemoveAt_m93FCD13B380DF3867D816269666754C5876AC929_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_get_Count_m5BA8B6A985C4ECE8C3EED6F7425B52AAB9BF2870_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_get_Item_m58FDF3017420D16492329082390353B1A031618A_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral598763C97CAEC44F4B760460963CD4730E60B870);
		s_Il2CppMethodInitialized = true;
	}
	String_t* V_0 = NULL;
	List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* V_1 = NULL;
	int32_t V_2 = 0;
	List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* G_B14_0 = NULL;
	SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* G_B14_1 = NULL;
	List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* G_B13_0 = NULL;
	SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* G_B13_1 = NULL;
	int32_t G_B15_0 = 0;
	List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* G_B15_1 = NULL;
	SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* G_B15_2 = NULL;
	int32_t G_B17_0 = 0;
	List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* G_B17_1 = NULL;
	SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* G_B17_2 = NULL;
	int32_t G_B16_0 = 0;
	List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* G_B16_1 = NULL;
	SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* G_B16_2 = NULL;
	int32_t G_B18_0 = 0;
	int32_t G_B18_1 = 0;
	List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* G_B18_2 = NULL;
	SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* G_B18_3 = NULL;
	{
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_0 = __this->____stars;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_0022;
		}
	}
	{
		String_t* L_2 = __this->____bakedTier;
		String_t* L_3 = __this->___Tier;
		bool L_4;
		L_4 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_2, L_3, NULL);
		if (!L_4)
		{
			goto IL_0022;
		}
	}
	{
		return;
	}

IL_0022:
	{
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		String_t* L_5;
		L_5 = SkyRig_ReadCatalog_m9B05C52846802B3200329BAF7CD93204CA621821(NULL);
		V_0 = L_5;
		String_t* L_6 = V_0;
		bool L_7;
		L_7 = String_IsNullOrEmpty_mEA9E3FB005AC28FE02E69FCF95A7B8456192B478(L_6, NULL);
		if (!L_7)
		{
			goto IL_0031;
		}
	}
	{
		return;
	}

IL_0031:
	{
		String_t* L_8 = V_0;
		String_t* L_9 = __this->___Tier;
		List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* L_10;
		L_10 = SkyBodies_LoadStars_m7A9416B647B668B98A44700FC25C2CC0EAD786A7(L_8, L_9, NULL);
		V_1 = L_10;
		List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* L_11 = V_1;
		NullCheck(L_11);
		int32_t L_12;
		L_12 = List_1_get_Count_m5BA8B6A985C4ECE8C3EED6F7425B52AAB9BF2870_inline(L_11, List_1_get_Count_m5BA8B6A985C4ECE8C3EED6F7425B52AAB9BF2870_RuntimeMethod_var);
		V_2 = ((int32_t)il2cpp_codegen_subtract(L_12, 1));
		goto IL_006b;
	}

IL_0049:
	{
		List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* L_13 = V_1;
		int32_t L_14 = V_2;
		NullCheck(L_13);
		StarPoint_t8DA928CE6AE24DC5827194FA6BD3B437EC92A3CE L_15;
		L_15 = List_1_get_Item_m58FDF3017420D16492329082390353B1A031618A(L_13, L_14, List_1_get_Item_m58FDF3017420D16492329082390353B1A031618A_RuntimeMethod_var);
		double L_16 = L_15.___VMag;
		if ((!(((double)L_16) > ((double)(6.0)))))
		{
			goto IL_0067;
		}
	}
	{
		List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* L_17 = V_1;
		int32_t L_18 = V_2;
		NullCheck(L_17);
		List_1_RemoveAt_m93FCD13B380DF3867D816269666754C5876AC929(L_17, L_18, List_1_RemoveAt_m93FCD13B380DF3867D816269666754C5876AC929_RuntimeMethod_var);
	}

IL_0067:
	{
		int32_t L_19 = V_2;
		V_2 = ((int32_t)il2cpp_codegen_subtract(L_19, 1));
	}

IL_006b:
	{
		int32_t L_20 = V_2;
		if ((((int32_t)L_20) >= ((int32_t)0)))
		{
			goto IL_0049;
		}
	}
	{
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_21 = __this->____stars;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_22;
		L_22 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_21, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_22)
		{
			goto IL_0088;
		}
	}
	{
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_23 = __this->____stars;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		Object_Destroy_mE97D0A766419A81296E8D4E5C23D01D3FE91ACBB(L_23, NULL);
	}

IL_0088:
	{
		List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* L_24 = V_1;
		String_t* L_25 = __this->___Tier;
		bool L_26;
		L_26 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_25, _stringLiteral598763C97CAEC44F4B760460963CD4730E60B870, NULL);
		if (L_26)
		{
			G_B14_0 = L_24;
			G_B14_1 = __this;
			goto IL_00a3;
		}
		G_B13_0 = L_24;
		G_B13_1 = __this;
	}
	{
		G_B15_0 = ((int32_t)512);
		G_B15_1 = G_B13_0;
		G_B15_2 = G_B13_1;
		goto IL_00a8;
	}

IL_00a3:
	{
		G_B15_0 = ((int32_t)256);
		G_B15_1 = G_B14_0;
		G_B15_2 = G_B14_1;
	}

IL_00a8:
	{
		String_t* L_27 = __this->___Tier;
		bool L_28;
		L_28 = String_op_Equality_m030E1B219352228970A076136E455C4E568C02C1(L_27, _stringLiteral598763C97CAEC44F4B760460963CD4730E60B870, NULL);
		if (L_28)
		{
			G_B17_0 = G_B15_0;
			G_B17_1 = G_B15_1;
			G_B17_2 = G_B15_2;
			goto IL_00c1;
		}
		G_B16_0 = G_B15_0;
		G_B16_1 = G_B15_1;
		G_B16_2 = G_B15_2;
	}
	{
		G_B18_0 = ((int32_t)256);
		G_B18_1 = G_B16_0;
		G_B18_2 = G_B16_1;
		G_B18_3 = G_B16_2;
		goto IL_00c6;
	}

IL_00c1:
	{
		G_B18_0 = ((int32_t)128);
		G_B18_1 = G_B17_0;
		G_B18_2 = G_B17_1;
		G_B18_3 = G_B17_2;
	}

IL_00c6:
	{
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_29;
		L_29 = StarBake_Bake_m6581337B736F5EEB31BB6C44F7BB503EAD7A6DBE(G_B18_2, G_B18_1, G_B18_0, NULL);
		NullCheck(G_B18_3);
		G_B18_3->____stars = L_29;
		Il2CppCodeGenWriteBarrier((void**)(&G_B18_3->____stars), (void*)L_29);
		String_t* L_30 = __this->___Tier;
		__this->____bakedTier = L_30;
		Il2CppCodeGenWriteBarrier((void**)(&__this->____bakedTier), (void*)L_30);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_31 = __this->____mat;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_32;
		L_32 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_31, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_32)
		{
			goto IL_010e;
		}
	}
	{
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_33 = __this->____stars;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_34;
		L_34 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_33, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_34)
		{
			goto IL_010e;
		}
	}
	{
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_35 = __this->____mat;
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		int32_t L_36 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___StarsId;
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_37 = __this->____stars;
		NullCheck(L_35);
		Material_SetTexture_mA9F8461850AAB88F992E9C6FA6F24C2E050B83FD(L_35, L_36, L_37, NULL);
	}

IL_010e:
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* SkyRig_ReadCatalog_m9B05C52846802B3200329BAF7CD93204CA621821 (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Application_tDB03BE91CDF0ACA614A5E0B67CFB77C44EB19B21_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Path_t8A38A801D0219E8209C1B1D90D82D4D755D998BC_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Resources_Load_TisTextAsset_t2C64E93DA366D9DE5A8209E1802FA4884AC1BD69_m55E770DF81AB6D40763121667DA5E743EF1036B2_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral382032B1217B79CEF593C94296F57E9FF1FCD22E);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralA44A39671D4B7FA8FBE50D795EAB52248D5C5469);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralE02BF054B0244C35F8DF7A02F4B8E98D37929BB1);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralF7C6D910CA12BFA2747F6EC5B8BAE314840807FA);
		s_Il2CppMethodInitialized = true;
	}
	TextAsset_t2C64E93DA366D9DE5A8209E1802FA4884AC1BD69* V_0 = NULL;
	String_t* V_1 = NULL;
	String_t* V_2 = NULL;
	String_t* V_3 = NULL;
	il2cpp::utils::ExceptionSupportStack<RuntimeObject*, 1> __active_exceptions;
	{
		TextAsset_t2C64E93DA366D9DE5A8209E1802FA4884AC1BD69* L_0;
		L_0 = Resources_Load_TisTextAsset_t2C64E93DA366D9DE5A8209E1802FA4884AC1BD69_m55E770DF81AB6D40763121667DA5E743EF1036B2(_stringLiteralE02BF054B0244C35F8DF7A02F4B8E98D37929BB1, Resources_Load_TisTextAsset_t2C64E93DA366D9DE5A8209E1802FA4884AC1BD69_m55E770DF81AB6D40763121667DA5E743EF1036B2_RuntimeMethod_var);
		V_0 = L_0;
		TextAsset_t2C64E93DA366D9DE5A8209E1802FA4884AC1BD69* L_1 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_2;
		L_2 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_1, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_2)
		{
			goto IL_0028;
		}
	}
	{
		TextAsset_t2C64E93DA366D9DE5A8209E1802FA4884AC1BD69* L_3 = V_0;
		NullCheck(L_3);
		String_t* L_4;
		L_4 = TextAsset_get_text_m36846042E3CF3D9DD337BF3F8B2B1902D10C8FD9(L_3, NULL);
		bool L_5;
		L_5 = String_IsNullOrEmpty_mEA9E3FB005AC28FE02E69FCF95A7B8456192B478(L_4, NULL);
		if (L_5)
		{
			goto IL_0028;
		}
	}
	{
		TextAsset_t2C64E93DA366D9DE5A8209E1802FA4884AC1BD69* L_6 = V_0;
		NullCheck(L_6);
		String_t* L_7;
		L_7 = TextAsset_get_text_m36846042E3CF3D9DD337BF3F8B2B1902D10C8FD9(L_6, NULL);
		return L_7;
	}

IL_0028:
	{
	}
	try
	{
		{
			il2cpp_codegen_runtime_class_init_inline(Application_tDB03BE91CDF0ACA614A5E0B67CFB77C44EB19B21_il2cpp_TypeInfo_var);
			String_t* L_8;
			L_8 = Application_get_dataPath_m4C8412CBEE4EAAAB6711CC9BEFFA73CEE5BDBEF7(NULL);
			String_t* L_9;
			L_9 = Catalog_FindRepoRoot_mEFA3BEDD8D988CD5669DCD08A60D512D6D5113AD(L_8, NULL);
			il2cpp_codegen_runtime_class_init_inline(Path_t8A38A801D0219E8209C1B1D90D82D4D755D998BC_il2cpp_TypeInfo_var);
			String_t* L_10;
			L_10 = Path_Combine_m3465FB3E62CC193B0B916386921C8AA72026FD13(L_9, _stringLiteralA44A39671D4B7FA8FBE50D795EAB52248D5C5469, _stringLiteral382032B1217B79CEF593C94296F57E9FF1FCD22E, _stringLiteralF7C6D910CA12BFA2747F6EC5B8BAE314840807FA, NULL);
			V_2 = L_10;
			String_t* L_11 = V_2;
			bool L_12;
			L_12 = File_Exists_m95E329ABBE3EAD6750FE1989BBA6884457136D4A(L_11, NULL);
			if (!L_12)
			{
				goto IL_0059_1;
			}
		}
		{
			String_t* L_13 = V_2;
			String_t* L_14;
			L_14 = File_ReadAllText_mA4A939F853D573379F7129AFDC469B91E9747BAA(L_13, NULL);
			V_3 = L_14;
			goto IL_0086;
		}

IL_0059_1:
		{
			goto IL_005e;
		}
	}
	catch(Il2CppExceptionWrapper& e)
	{
		if(il2cpp_codegen_class_is_assignable_from (((RuntimeClass*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&Exception_t_il2cpp_TypeInfo_var)), il2cpp_codegen_object_class(e.ex)))
		{
			IL2CPP_PUSH_ACTIVE_EXCEPTION(e.ex);
			goto CATCH_005b;
		}
		throw e;
	}

CATCH_005b:
	{
		Exception_t* L_15 = ((Exception_t*)IL2CPP_GET_ACTIVE_EXCEPTION(Exception_t*));;
		IL2CPP_POP_ACTIVE_EXCEPTION(Exception_t*);
		goto IL_005e;
	}

IL_005e:
	{
		il2cpp_codegen_runtime_class_init_inline(Application_tDB03BE91CDF0ACA614A5E0B67CFB77C44EB19B21_il2cpp_TypeInfo_var);
		String_t* L_16;
		L_16 = Application_get_streamingAssetsPath_mB904BCD9A7A4F18A52C175DE4A81F5DC3010CDB5(NULL);
		il2cpp_codegen_runtime_class_init_inline(Path_t8A38A801D0219E8209C1B1D90D82D4D755D998BC_il2cpp_TypeInfo_var);
		String_t* L_17;
		L_17 = Path_Combine_m1ADAC05CDA2D1D61B172DF65A81E86592696BEAE(L_16, _stringLiteralF7C6D910CA12BFA2747F6EC5B8BAE314840807FA, NULL);
		V_1 = L_17;
	}
	try
	{
		{
			String_t* L_18 = V_1;
			bool L_19;
			L_19 = File_Exists_m95E329ABBE3EAD6750FE1989BBA6884457136D4A(L_18, NULL);
			if (!L_19)
			{
				goto IL_007f_1;
			}
		}
		{
			String_t* L_20 = V_1;
			String_t* L_21;
			L_21 = File_ReadAllText_mA4A939F853D573379F7129AFDC469B91E9747BAA(L_20, NULL);
			V_3 = L_21;
			goto IL_0086;
		}

IL_007f_1:
		{
			goto IL_0084;
		}
	}
	catch(Il2CppExceptionWrapper& e)
	{
		if(il2cpp_codegen_class_is_assignable_from (((RuntimeClass*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&Exception_t_il2cpp_TypeInfo_var)), il2cpp_codegen_object_class(e.ex)))
		{
			IL2CPP_PUSH_ACTIVE_EXCEPTION(e.ex);
			goto CATCH_0081;
		}
		throw e;
	}

CATCH_0081:
	{
		Exception_t* L_22 = ((Exception_t*)IL2CPP_GET_ACTIVE_EXCEPTION(Exception_t*));;
		IL2CPP_POP_ACTIVE_EXCEPTION(Exception_t*);
		goto IL_0084;
	}

IL_0084:
	{
		return (String_t*)NULL;
	}

IL_0086:
	{
		String_t* L_23 = V_3;
		return L_23;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool SkyRig_IsErrorShader_mC4CDA81F46C7A7F50E9979F84005B73B429AF45A (Shader_tADC867D36B7876EE22427FAA2CE485105F4EE692* ___0_shader, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral173E461F0DC20C90ABC76BD2F0A45B5DBCA33335);
		s_Il2CppMethodInitialized = true;
	}
	String_t* V_0 = NULL;
	{
		Shader_tADC867D36B7876EE22427FAA2CE485105F4EE692* L_0 = ___0_shader;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_000b;
		}
	}
	{
		return (bool)1;
	}

IL_000b:
	{
		Shader_tADC867D36B7876EE22427FAA2CE485105F4EE692* L_2 = ___0_shader;
		NullCheck(L_2);
		String_t* L_3;
		L_3 = Object_get_name_mAC2F6B897CF1303BA4249B4CB55271AFACBB6392(L_2, NULL);
		V_0 = L_3;
		String_t* L_4 = V_0;
		bool L_5;
		L_5 = String_IsNullOrEmpty_mEA9E3FB005AC28FE02E69FCF95A7B8456192B478(L_4, NULL);
		if (L_5)
		{
			goto IL_002d;
		}
	}
	{
		String_t* L_6 = V_0;
		NullCheck(L_6);
		int32_t L_7;
		L_7 = String_IndexOf_m490810CB7ADA9230AC0F8D78E213A8EFED129F55(L_6, _stringLiteral173E461F0DC20C90ABC76BD2F0A45B5DBCA33335, 5, NULL);
		return (bool)((((int32_t)((((int32_t)L_7) < ((int32_t)0))? 1 : 0)) == ((int32_t)0))? 1 : 0);
	}

IL_002d:
	{
		return (bool)0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_Apply_m335778822D8B098D9CC60D14674773698488EAAD (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C ___0_g, SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D ___1_sun, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkySun_t55412B2FF59478EDEEE01F79F3B0E738516396BD_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	Color_tD001788D726C3A7F1379BEED0260B9591F440C1F V_0;
	memset((&V_0), 0, sizeof(V_0));
	double V_1 = 0.0;
	double V_2 = 0.0;
	SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D V_3;
	memset((&V_3), 0, sizeof(V_3));
	SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D V_4;
	memset((&V_4), 0, sizeof(V_4));
	SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D V_5;
	memset((&V_5), 0, sizeof(V_5));
	float V_6 = 0.0f;
	float G_B9_0 = 0.0f;
	float G_B9_1 = 0.0f;
	float G_B9_2 = 0.0f;
	int32_t G_B9_3 = 0;
	Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* G_B9_4 = NULL;
	float G_B8_0 = 0.0f;
	float G_B8_1 = 0.0f;
	float G_B8_2 = 0.0f;
	int32_t G_B8_3 = 0;
	Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* G_B8_4 = NULL;
	float G_B10_0 = 0.0f;
	float G_B10_1 = 0.0f;
	float G_B10_2 = 0.0f;
	float G_B10_3 = 0.0f;
	int32_t G_B10_4 = 0;
	Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* G_B10_5 = NULL;
	float G_B12_0 = 0.0f;
	float G_B12_1 = 0.0f;
	float G_B12_2 = 0.0f;
	int32_t G_B12_3 = 0;
	Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* G_B12_4 = NULL;
	float G_B11_0 = 0.0f;
	float G_B11_1 = 0.0f;
	float G_B11_2 = 0.0f;
	int32_t G_B11_3 = 0;
	Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* G_B11_4 = NULL;
	float G_B13_0 = 0.0f;
	float G_B13_1 = 0.0f;
	float G_B13_2 = 0.0f;
	float G_B13_3 = 0.0f;
	int32_t G_B13_4 = 0;
	Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* G_B13_5 = NULL;
	{
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_0 = __this->____mat;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_0032;
		}
	}
	{
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_2 = __this->____mat;
		NullCheck(L_2);
		Shader_tADC867D36B7876EE22427FAA2CE485105F4EE692* L_3;
		L_3 = Material_get_shader_m8B0C11AE6F2AD7DE30AF52D3195EB716F7A71983(L_2, NULL);
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		bool L_4;
		L_4 = SkyRig_IsErrorShader_mC4CDA81F46C7A7F50E9979F84005B73B429AF45A(L_3, NULL);
		if (!L_4)
		{
			goto IL_0032;
		}
	}
	{
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_5 = __this->____mat;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		Object_Destroy_mE97D0A766419A81296E8D4E5C23D01D3FE91ACBB(L_5, NULL);
		__this->____mat = (Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3*)NULL;
		Il2CppCodeGenWriteBarrier((void**)(&__this->____mat), (void*)(Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3*)NULL);
	}

IL_0032:
	{
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_6 = ___0_g;
		Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_7 = L_6.___Horizon;
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_8;
		L_8 = SkyRig_Display_m5B3BB5D54D4BC9D33F8865240C9091D2CD32F4D4(L_7, NULL);
		V_0 = L_8;
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_9 = __this->____mat;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_10;
		L_10 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_9, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_10)
		{
			goto IL_0082;
		}
	}
	{
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_11 = __this->___View;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_12;
		L_12 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_11, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_12)
		{
			goto IL_0072;
		}
	}
	{
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_13 = __this->___View;
		NullCheck(L_13);
		Camera_set_clearFlags_m66541D9CC43CBAA5FE7364A50D43CA5569FD4D93(L_13, 2, NULL);
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_14 = __this->___View;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_15 = V_0;
		NullCheck(L_14);
		Camera_set_backgroundColor_m036FD8C316A93A0B168ACC89AFF16D396B872138(L_14, L_15, NULL);
	}

IL_0072:
	{
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_16 = ___0_g;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_17 = V_0;
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		SkyRig_ApplyFog_m683B5D221A6F7E19D876507522078CA3380663D0(L_16, L_17, NULL);
		SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D L_18 = ___1_sun;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_19 = ___0_g;
		SkyRig_AimSun_m5FAFDB6D014B5F14E368CD8C381031DD402B90C0(__this, L_18, L_19, NULL);
		return;
	}

IL_0082:
	{
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_20 = __this->____mat;
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		int32_t L_21 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___ZenithId;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_22 = ___0_g;
		Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_23 = L_22.___Zenith;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_24;
		L_24 = SkyRig_Display_m5B3BB5D54D4BC9D33F8865240C9091D2CD32F4D4(L_23, NULL);
		NullCheck(L_20);
		Material_SetColor_m573C88F2FB1B5A978C53A197B414F9E9C6AC5B9A(L_20, L_21, L_24, NULL);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_25 = __this->____mat;
		int32_t L_26 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___HorizonId;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_27 = V_0;
		NullCheck(L_25);
		Material_SetColor_m573C88F2FB1B5A978C53A197B414F9E9C6AC5B9A(L_25, L_26, L_27, NULL);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_28 = __this->____mat;
		int32_t L_29 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___LimbId;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_30 = ___0_g;
		Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_31 = L_30.___Limb;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_32;
		L_32 = SkyRig_Display_m5B3BB5D54D4BC9D33F8865240C9091D2CD32F4D4(L_31, NULL);
		NullCheck(L_28);
		Material_SetColor_m573C88F2FB1B5A978C53A197B414F9E9C6AC5B9A(L_28, L_29, L_32, NULL);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_33 = __this->____mat;
		int32_t L_34 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___GroundId;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_35 = ___0_g;
		Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_36 = L_35.___Ground;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_37;
		L_37 = SkyRig_Display_m5B3BB5D54D4BC9D33F8865240C9091D2CD32F4D4(L_36, NULL);
		NullCheck(L_33);
		Material_SetColor_m573C88F2FB1B5A978C53A197B414F9E9C6AC5B9A(L_33, L_34, L_37, NULL);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_38 = __this->____mat;
		int32_t L_39 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___SunColorId;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_40 = ___0_g;
		Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_41 = L_40.___Sun;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_42;
		L_42 = SkyRig_Display_m5B3BB5D54D4BC9D33F8865240C9091D2CD32F4D4(L_41, NULL);
		NullCheck(L_38);
		Material_SetColor_m573C88F2FB1B5A978C53A197B414F9E9C6AC5B9A(L_38, L_39, L_42, NULL);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_43 = __this->____mat;
		int32_t L_44 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___SunDirId;
		SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D L_45 = ___1_sun;
		double L_46 = L_45.___X;
		SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D L_47 = ___1_sun;
		double L_48 = L_47.___Y;
		SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D L_49 = ___1_sun;
		double L_50 = L_49.___Z;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_51 = ___0_g;
		double L_52 = L_51.___SunDisc;
		Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 L_53;
		memset((&L_53), 0, sizeof(L_53));
		Vector4__ctor_m96B2CD8B862B271F513AF0BDC2EABD58E4DBC813_inline((&L_53), ((float)L_46), ((float)L_48), ((float)L_50), ((float)L_52), NULL);
		NullCheck(L_43);
		Material_SetVector_m44CD02D4555E2AF391C30700F0AEC36BA04CFEA7(L_43, L_44, L_53, NULL);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_54 = __this->____mat;
		int32_t L_55 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___ParamsId;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_56 = ___0_g;
		double L_57 = L_56.___Haze;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_58 = ___0_g;
		double L_59 = L_58.___StarAlpha;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_60 = ___0_g;
		double L_61 = L_60.___SunGlow;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_62 = ___0_g;
		double L_63 = L_62.___SunDisc;
		Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 L_64;
		memset((&L_64), 0, sizeof(L_64));
		Vector4__ctor_m96B2CD8B862B271F513AF0BDC2EABD58E4DBC813_inline((&L_64), ((float)L_57), ((float)L_59), ((float)L_61), ((float)L_63), NULL);
		NullCheck(L_54);
		Material_SetVector_m44CD02D4555E2AF391C30700F0AEC36BA04CFEA7(L_54, L_55, L_64, NULL);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_65 = __this->____mat;
		int32_t L_66 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___MilkyId;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_67 = ___0_g;
		double L_68 = L_67.___Milky;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_69 = ___0_g;
		double L_70 = L_69.___Airglow;
		float L_71;
		L_71 = Time_get_time_m3A271BB1B20041144AC5B7863B71AB1F0150374B(NULL);
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_72 = __this->____stars;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_73;
		L_73 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_72, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (L_73)
		{
			G_B9_0 = L_71;
			G_B9_1 = ((float)L_70);
			G_B9_2 = ((float)L_68);
			G_B9_3 = L_66;
			G_B9_4 = L_65;
			goto IL_0194;
		}
		G_B8_0 = L_71;
		G_B8_1 = ((float)L_70);
		G_B8_2 = ((float)L_68);
		G_B8_3 = L_66;
		G_B8_4 = L_65;
	}
	{
		G_B10_0 = (0.0f);
		G_B10_1 = G_B8_0;
		G_B10_2 = G_B8_1;
		G_B10_3 = G_B8_2;
		G_B10_4 = G_B8_3;
		G_B10_5 = G_B8_4;
		goto IL_0199;
	}

IL_0194:
	{
		G_B10_0 = (1.0f);
		G_B10_1 = G_B9_0;
		G_B10_2 = G_B9_1;
		G_B10_3 = G_B9_2;
		G_B10_4 = G_B9_3;
		G_B10_5 = G_B9_4;
	}

IL_0199:
	{
		Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 L_74;
		memset((&L_74), 0, sizeof(L_74));
		Vector4__ctor_m96B2CD8B862B271F513AF0BDC2EABD58E4DBC813_inline((&L_74), G_B10_3, G_B10_2, G_B10_1, G_B10_0, NULL);
		NullCheck(G_B10_5);
		Material_SetVector_m44CD02D4555E2AF391C30700F0AEC36BA04CFEA7(G_B10_5, G_B10_4, L_74, NULL);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_75 = __this->____mat;
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		int32_t L_76 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___CloudAId;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_77 = ___0_g;
		double L_78 = L_77.___Cloud0;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_79 = ___0_g;
		double L_80 = L_79.___Cloud1;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_81 = ___0_g;
		double L_82 = L_81.___Cloud2;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_83 = ___0_g;
		double L_84 = L_83.___CloudAlpha;
		Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 L_85;
		memset((&L_85), 0, sizeof(L_85));
		Vector4__ctor_m96B2CD8B862B271F513AF0BDC2EABD58E4DBC813_inline((&L_85), ((float)L_78), ((float)L_80), ((float)L_82), ((float)L_84), NULL);
		NullCheck(L_75);
		Material_SetVector_m44CD02D4555E2AF391C30700F0AEC36BA04CFEA7(L_75, L_76, L_85, NULL);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_86 = __this->____mat;
		int32_t L_87 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___CloudHId;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_88 = ___0_g;
		double L_89 = L_88.___Height0;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_90 = ___0_g;
		double L_91 = L_90.___Height1;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_92 = ___0_g;
		double L_93 = L_92.___Height2;
		double L_94 = __this->____altitudeM;
		Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 L_95;
		memset((&L_95), 0, sizeof(L_95));
		Vector4__ctor_m96B2CD8B862B271F513AF0BDC2EABD58E4DBC813_inline((&L_95), ((float)L_89), ((float)L_91), ((float)L_93), ((float)L_94), NULL);
		NullCheck(L_86);
		Material_SetVector_m44CD02D4555E2AF391C30700F0AEC36BA04CFEA7(L_86, L_87, L_95, NULL);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_96 = __this->____mat;
		int32_t L_97 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___WindId;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_98 = ___0_g;
		double L_99 = L_98.___WindEast;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_100 = ___0_g;
		double L_101 = L_100.___WindNorth;
		SkyWeather_t905CF34969B44F09A45D96F10FA58C6D01AE2DA2* L_102 = (SkyWeather_t905CF34969B44F09A45D96F10FA58C6D01AE2DA2*)(&__this->___Weather);
		bool L_103 = L_102->___Precip;
		if (L_103)
		{
			G_B12_0 = (0.000119999997f);
			G_B12_1 = ((float)L_101);
			G_B12_2 = ((float)L_99);
			G_B12_3 = L_97;
			G_B12_4 = L_96;
			goto IL_0237;
		}
		G_B11_0 = (0.000119999997f);
		G_B11_1 = ((float)L_101);
		G_B11_2 = ((float)L_99);
		G_B11_3 = L_97;
		G_B11_4 = L_96;
	}
	{
		G_B13_0 = (0.0f);
		G_B13_1 = G_B11_0;
		G_B13_2 = G_B11_1;
		G_B13_3 = G_B11_2;
		G_B13_4 = G_B11_3;
		G_B13_5 = G_B11_4;
		goto IL_023c;
	}

IL_0237:
	{
		G_B13_0 = (1.0f);
		G_B13_1 = G_B12_0;
		G_B13_2 = G_B12_1;
		G_B13_3 = G_B12_2;
		G_B13_4 = G_B12_3;
		G_B13_5 = G_B12_4;
	}

IL_023c:
	{
		Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 L_104;
		memset((&L_104), 0, sizeof(L_104));
		Vector4__ctor_m96B2CD8B862B271F513AF0BDC2EABD58E4DBC813_inline((&L_104), G_B13_3, G_B13_2, G_B13_1, G_B13_0, NULL);
		NullCheck(G_B13_5);
		Material_SetVector_m44CD02D4555E2AF391C30700F0AEC36BA04CFEA7(G_B13_5, G_B13_4, L_104, NULL);
		int32_t L_105 = __this->___Year;
		int32_t L_106 = __this->___Month;
		int32_t L_107 = __this->___Day;
		double L_108 = __this->___HoursUtc;
		Frames_JdUt1Parts_m600EE3044F3D0E1F3B1EA85B22EE165DF6DF13CA(L_105, L_106, L_107, L_108, (&V_1), (&V_2), NULL);
		double L_109 = __this->___LatitudeDeg;
		double L_110 = __this->___LongitudeDeg;
		double L_111 = V_1;
		double L_112 = V_2;
		SkyBodies_EquatorialAxes_m1979046ACA70599109036BCD884367E0A7E4B1AC(L_109, L_110, L_111, L_112, (&V_3), (&V_4), (&V_5), NULL);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_113 = __this->____mat;
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		int32_t L_114 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___Eq0Id;
		SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D L_115 = V_3;
		Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 L_116;
		L_116 = SkyRig_Vec_m941B9A7A51ED8C05D5C250649238DD8B2E15F055(L_115, NULL);
		NullCheck(L_113);
		Material_SetVector_m44CD02D4555E2AF391C30700F0AEC36BA04CFEA7(L_113, L_114, L_116, NULL);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_117 = __this->____mat;
		int32_t L_118 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___Eq1Id;
		SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D L_119 = V_4;
		Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 L_120;
		L_120 = SkyRig_Vec_m941B9A7A51ED8C05D5C250649238DD8B2E15F055(L_119, NULL);
		NullCheck(L_117);
		Material_SetVector_m44CD02D4555E2AF391C30700F0AEC36BA04CFEA7(L_117, L_118, L_120, NULL);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_121 = __this->____mat;
		int32_t L_122 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___Eq2Id;
		SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D L_123 = V_5;
		Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 L_124;
		L_124 = SkyRig_Vec_m941B9A7A51ED8C05D5C250649238DD8B2E15F055(L_123, NULL);
		NullCheck(L_121);
		Material_SetVector_m44CD02D4555E2AF391C30700F0AEC36BA04CFEA7(L_121, L_122, L_124, NULL);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_125 = __this->____mat;
		int32_t L_126 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___GalNId;
		SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D L_127;
		L_127 = SkyBodies_GalacticNorthEquatorial_m49B8A340BCC81620E8805E42D4675EE298751AE0(NULL);
		Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 L_128;
		L_128 = SkyRig_Vec_m941B9A7A51ED8C05D5C250649238DD8B2E15F055(L_127, NULL);
		NullCheck(L_125);
		Material_SetVector_m44CD02D4555E2AF391C30700F0AEC36BA04CFEA7(L_125, L_126, L_128, NULL);
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_129 = __this->____stars;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_130;
		L_130 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_129, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_130)
		{
			goto IL_0339;
		}
	}
	{
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_131 = __this->____mat;
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		int32_t L_132 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___StarsId;
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_133 = __this->____stars;
		NullCheck(L_131);
		Material_SetTexture_mA9F8461850AAB88F992E9C6FA6F24C2E050B83FD(L_131, L_132, L_133, NULL);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_134 = __this->____mat;
		int32_t L_135 = ((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___StarTexId;
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_136 = __this->____stars;
		NullCheck(L_136);
		int32_t L_137;
		L_137 = VirtualFuncInvoker0< int32_t >::Invoke(5, L_136);
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_138 = __this->____stars;
		NullCheck(L_138);
		int32_t L_139;
		L_139 = VirtualFuncInvoker0< int32_t >::Invoke(7, L_138);
		Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 L_140;
		memset((&L_140), 0, sizeof(L_140));
		Vector4__ctor_m96B2CD8B862B271F513AF0BDC2EABD58E4DBC813_inline((&L_140), ((float)L_137), ((float)L_139), (0.0f), (0.0f), NULL);
		NullCheck(L_134);
		Material_SetVector_m44CD02D4555E2AF391C30700F0AEC36BA04CFEA7(L_134, L_135, L_140, NULL);
	}

IL_0339:
	{
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_141 = __this->___View;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_142;
		L_142 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_141, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_142)
		{
			goto IL_035f;
		}
	}
	{
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_143 = __this->___View;
		NullCheck(L_143);
		Camera_set_clearFlags_m66541D9CC43CBAA5FE7364A50D43CA5569FD4D93(L_143, 1, NULL);
		Camera_tA92CC927D7439999BC82DBEDC0AA45B470F9E184* L_144 = __this->___View;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_145 = V_0;
		NullCheck(L_144);
		Camera_set_backgroundColor_m036FD8C316A93A0B168ACC89AFF16D396B872138(L_144, L_145, NULL);
	}

IL_035f:
	{
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_146 = __this->____mat;
		RenderSettings_set_skybox_m7988CC3217B1456CA0A7294127527BFC18EEC872(L_146, NULL);
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_147 = ___0_g;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_148 = V_0;
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		SkyRig_ApplyFog_m683B5D221A6F7E19D876507522078CA3380663D0(L_147, L_148, NULL);
		SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D L_149 = ___1_sun;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_150 = ___0_g;
		SkyRig_AimSun_m5FAFDB6D014B5F14E368CD8C381031DD402B90C0(__this, L_149, L_150, NULL);
		il2cpp_codegen_runtime_class_init_inline(SkySun_t55412B2FF59478EDEEE01F79F3B0E738516396BD_il2cpp_TypeInfo_var);
		float L_151;
		L_151 = SkySun_NightFactor_m8D68B457C71134AFA96E7C36F0F3267125D2674D(NULL);
		V_6 = ((float)il2cpp_codegen_subtract((1.0f), L_151));
		RenderSettings_set_ambientMode_mDC8C4F62B1BCDDBFA69E9398213B926E39D512FC(1, NULL);
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_152 = ___0_g;
		Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_153 = L_152.___Zenith;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_154;
		L_154 = SkyRig_Display_m5B3BB5D54D4BC9D33F8865240C9091D2CD32F4D4(L_153, NULL);
		RenderSettings_set_ambientSkyColor_m5C39982DA79150786D1C220A98EF7925CD2714CE(L_154, NULL);
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_155 = V_0;
		RenderSettings_set_ambientEquatorColor_mF032192969C1E56A8A67A210BFC27C98DCB8CADB(L_155, NULL);
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_156 = ___0_g;
		Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_157 = L_156.___Ground;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_158;
		L_158 = SkyRig_Display_m5B3BB5D54D4BC9D33F8865240C9091D2CD32F4D4(L_157, NULL);
		RenderSettings_set_ambientGroundColor_m78EC24258C1F55C474321A66A4C73742E7082850(L_158, NULL);
		float L_159 = V_6;
		float L_160;
		L_160 = Mathf_Lerp_m47EF2FFB7647BD0A1FDC26DC03E28B19812139B5_inline((0.150000006f), (1.0f), L_159, NULL);
		RenderSettings_set_ambientIntensity_m27875436E47E2D28C13F362DAE9C24D0DEC4A844(L_160, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_ApplyFog_m683B5D221A6F7E19D876507522078CA3380663D0 (SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C ___0_g, Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___1_horizon, const RuntimeMethod* method) 
{
	{
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_0 = ___0_g;
		double L_1 = L_0.___Haze;
		RenderSettings_set_fog_m3CB80EC4A17400AF2D6D7CBE6B541E01D8E2AA0E((bool)((((double)L_1) > ((double)(0.02)))? 1 : 0), NULL);
		RenderSettings_set_fogMode_m9D67706DC8DB33B69479A6D47CEC2131E93363C0(1, NULL);
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_2 = ___1_horizon;
		RenderSettings_set_fogColor_mBBD12EC3492195F85F583CAF4FAE4A973227639B(L_2, NULL);
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_3 = ___0_g;
		double L_4 = L_3.___FogStartM;
		RenderSettings_set_fogStartDistance_mF53EDDD1B96FD948B5213BAC17480FEE847896CA(((float)L_4), NULL);
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_5 = ___0_g;
		double L_6 = L_5.___FogEndM;
		RenderSettings_set_fogEndDistance_m3A8CA92B0A13432069104061629C25DF13BAEDDD(((float)L_6), NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_AimSun_m5FAFDB6D014B5F14E368CD8C381031DD402B90C0 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D ___0_sun, SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C ___1_g, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkySun_t55412B2FF59478EDEEE01F79F3B0E738516396BD_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_0;
	memset((&V_0), 0, sizeof(V_0));
	float V_1 = 0.0f;
	float V_2 = 0.0f;
	{
		Light_t1E68479B7782AF2050FAA02A5DC612FD034F18F3* L_0 = __this->___SunLight;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_000f;
		}
	}
	{
		return;
	}

IL_000f:
	{
		SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D L_2 = ___0_sun;
		double L_3 = L_2.___X;
		SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D L_4 = ___0_sun;
		double L_5 = L_4.___Y;
		SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D L_6 = ___0_sun;
		double L_7 = L_6.___Z;
		Vector3__ctor_m376936E6B999EF1ECBE57D990A386303E2283DE0_inline((&V_0), ((float)L_3), ((float)L_5), ((float)L_7), NULL);
		float L_8;
		L_8 = Vector3_get_sqrMagnitude_m43C27DEC47C4811FB30AB474FF2131A963B66FC8_inline((&V_0), NULL);
		if ((!(((float)L_8) < ((float)(9.99999994E-09f)))))
		{
			goto IL_003f;
		}
	}
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_9;
		L_9 = Vector3_get_up_m128AF3FDC820BF59D5DE86D973E7DE3F20C3AEBA_inline(NULL);
		V_0 = L_9;
	}

IL_003f:
	{
		Light_t1E68479B7782AF2050FAA02A5DC612FD034F18F3* L_10 = __this->___SunLight;
		NullCheck(L_10);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_11;
		L_11 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(L_10, NULL);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_12;
		L_12 = Vector3_get_normalized_m736BBF65D5CDA7A18414370D15B4DFCC1E466F07_inline((&V_0), NULL);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_13;
		L_13 = Vector3_op_UnaryNegation_m5450829F333BD2A88AF9A592C4EE331661225915_inline(L_12, NULL);
		Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 L_14;
		L_14 = Quaternion_LookRotation_mDB2CCA75B8E1AB98104F2A6E1A1EA57D0D1298D7_inline(L_13, NULL);
		NullCheck(L_11);
		Transform_set_rotation_m61340DE74726CF0F9946743A727C4D444397331D(L_11, L_14, NULL);
		Light_t1E68479B7782AF2050FAA02A5DC612FD034F18F3* L_15 = __this->___SunLight;
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_16 = ___1_g;
		Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_17 = L_16.___Sun;
		il2cpp_codegen_runtime_class_init_inline(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_18;
		L_18 = SkyRig_Display_m5B3BB5D54D4BC9D33F8865240C9091D2CD32F4D4(L_17, NULL);
		NullCheck(L_15);
		Light_set_color_m242F09E22904E0A20D5B032A0749678B0796A87C(L_15, L_18, NULL);
		il2cpp_codegen_runtime_class_init_inline(SkySun_t55412B2FF59478EDEEE01F79F3B0E738516396BD_il2cpp_TypeInfo_var);
		float L_19;
		L_19 = SkySun_NightFactor_m8D68B457C71134AFA96E7C36F0F3267125D2674D(NULL);
		V_1 = ((float)il2cpp_codegen_subtract((1.0f), L_19));
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_20 = ___1_g;
		double L_21 = L_20.___Column;
		float L_22;
		L_22 = expf(((float)il2cpp_codegen_multiply(((-((float)L_21))), (0.600000024f))));
		V_2 = L_22;
		Light_t1E68479B7782AF2050FAA02A5DC612FD034F18F3* L_23 = __this->___SunLight;
		float L_24 = V_2;
		float L_25;
		L_25 = Mathf_Lerp_m47EF2FFB7647BD0A1FDC26DC03E28B19812139B5_inline((1.04999995f), (1.35000002f), L_24, NULL);
		float L_26 = V_1;
		float L_27;
		L_27 = Mathf_Lerp_m47EF2FFB7647BD0A1FDC26DC03E28B19812139B5_inline((0.0199999996f), L_25, L_26, NULL);
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_28 = ___1_g;
		double L_29 = L_28.___SunDisc;
		float L_30;
		L_30 = Mathf_Lerp_m47EF2FFB7647BD0A1FDC26DC03E28B19812139B5_inline((0.349999994f), (1.0f), ((float)il2cpp_codegen_add(((float)L_29), (0.150000006f))), NULL);
		NullCheck(L_23);
		Light_set_intensity_mE4820C7F39F490B92ED5EA0C3AADA7C0775BE854(L_23, ((float)il2cpp_codegen_multiply(L_27, L_30)), NULL);
		Light_t1E68479B7782AF2050FAA02A5DC612FD034F18F3* L_31 = __this->___SunLight;
		RenderSettings_set_sun_m3331D8BCAF24DFA30A2BB1CCDA6542958896FEAD(L_31, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 SkyRig_Vec_m941B9A7A51ED8C05D5C250649238DD8B2E15F055 (SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D ___0_v, const RuntimeMethod* method) 
{
	{
		SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D L_0 = ___0_v;
		double L_1 = L_0.___X;
		SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D L_2 = ___0_v;
		double L_3 = L_2.___Y;
		SkyVec_tAEBEE3CCACA5BBD358F78C75431CA39FD05FB60D L_4 = ___0_v;
		double L_5 = L_4.___Z;
		Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3 L_6;
		memset((&L_6), 0, sizeof(L_6));
		Vector4__ctor_m96B2CD8B862B271F513AF0BDC2EABD58E4DBC813_inline((&L_6), ((float)L_1), ((float)L_3), ((float)L_5), (0.0f), NULL);
		return L_6;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Color_tD001788D726C3A7F1379BEED0260B9591F440C1F SkyRig_Display_m5B3BB5D54D4BC9D33F8865240C9091D2CD32F4D4 (Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 ___0_c, const RuntimeMethod* method) 
{
	Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 V_0;
	memset((&V_0), 0, sizeof(V_0));
	Color_tD001788D726C3A7F1379BEED0260B9591F440C1F V_1;
	memset((&V_1), 0, sizeof(V_1));
	{
		Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_0;
		L_0 = Rgb_ToSrgb_m0C7E6A1067F5AAE700CE816C919291A9A9C4E8D4((&___0_c), NULL);
		V_0 = L_0;
		Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_1 = V_0;
		double L_2 = L_1.___R;
		Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_3 = V_0;
		double L_4 = L_3.___G;
		Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_5 = V_0;
		double L_6 = L_5.___B;
		Color__ctor_m3786F0D6E510D9CFA544523A955870BD2A514C8C_inline((&V_1), ((float)L_2), ((float)L_4), ((float)L_6), (1.0f), NULL);
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_7 = V_1;
		float L_8 = L_7.___r;
		if ((!(((float)L_8) > ((float)(0.899999976f)))))
		{
			goto IL_0056;
		}
	}
	{
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_9 = V_1;
		float L_10 = L_9.___b;
		if ((!(((float)L_10) > ((float)(0.899999976f)))))
		{
			goto IL_0056;
		}
	}
	{
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_11 = V_1;
		float L_12 = L_11.___g;
		if ((!(((float)L_12) < ((float)(0.200000003f)))))
		{
			goto IL_0056;
		}
	}
	{
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_13;
		L_13 = Color_get_black_mB50217951591A045844C61E7FF31EEE3FEF16737_inline(NULL);
		return L_13;
	}

IL_0056:
	{
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_14 = V_1;
		return L_14;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig_OnDestroy_m98E32B36FDDD36E87F794DF152D0C3F2BD6DF2F3 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_0 = __this->____stars;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_0019;
		}
	}
	{
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_2 = __this->____stars;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		Object_Destroy_mE97D0A766419A81296E8D4E5C23D01D3FE91ACBB(L_2, NULL);
	}

IL_0019:
	{
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_3 = __this->____mat;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_4;
		L_4 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_3, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_4)
		{
			goto IL_0032;
		}
	}
	{
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_5 = __this->____mat;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		Object_Destroy_mE97D0A766419A81296E8D4E5C23D01D3FE91ACBB(L_5, NULL);
	}

IL_0032:
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig__ctor_m3850B0D753C99E975373338B4057729DEF896F94 (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral4BD3052BB1EC7598F0952B3FA0F49095189128A3);
		s_Il2CppMethodInitialized = true;
	}
	{
		__this->___Tier = _stringLiteral4BD3052BB1EC7598F0952B3FA0F49095189128A3;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___Tier), (void*)_stringLiteral4BD3052BB1EC7598F0952B3FA0F49095189128A3);
		SkyArt_t9BEFE797BE1852E9A478879F377E2884E3F7A751 L_0;
		L_0 = SkyArt_get_Neutral_m41D00FAAB2812B17D162EEA12A406303A987E85E(NULL);
		__this->___Art = L_0;
		SkyWeather_t905CF34969B44F09A45D96F10FA58C6D01AE2DA2 L_1;
		L_1 = SkyWeather_get_Clear_m0951F86F60733CA40966791E0BF16A102EF60774(NULL);
		__this->___Weather = L_1;
		__this->___LatitudeDeg = (28.5);
		__this->___LongitudeDeg = (-80.599999999999994);
		__this->___Year = ((int32_t)2026);
		__this->___Month = 6;
		__this->___Day = ((int32_t)21);
		__this->___HoursUtc = (16.0);
		__this->____clockKey = (std::numeric_limits<double>::quiet_NaN());
		MonoBehaviour__ctor_m592DB0105CA0BC97AA1C5F4AD27B12D68A3B7C1E(__this, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SkyRig__cctor_m7A055D543E10AEEEF22229A1F366AF45C2485460 (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral06EA5078C0497FD5E98F2AC8A4ACBBF3AE62DDBC);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral145EC6B8C2BA623D11BDD6955CB20C5B0C766445);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral16571D1FE92CF04A8148F1652D0B353AD4471457);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral286F5A1B75EBF2E4DAD1784902DE57222F307786);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral32EFDDFEAA75BD8C9B227E8BC9CCE4B7268C0716);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral35C1F12C5BBBFC1B19520B74D959C91FA2FFD16D);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral59E9A90ACEB12BE1578CBAE545691B1979987312);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral682657B19B8755087EB65BB148A26CA596BDE3C9);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral6EEC04A3DFB247A18B195A7D12B42F30FFB94CAD);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral8EC2A8CF93EE6CCEE33C770888DD7080CDAC60E1);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral9C14B5BA0DE646558EC300FF192C1B945F94D345);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralA6AF09A2719F5DE17063C132A6811819EF06AC36);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralB9AEECB59BF43D9ACA81E7B9AEF765A7DB4BA263);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralBB816673BA1055A7FE7842C8E1DC06FAC9CCEA45);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralBD5B20F7F6017F0873E4EF984CB16D9DC6708E3E);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralF3C2A64A9C8933B4A8FBBDAFF40694249C19FAC9);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralF873D6748545B7516CD56A7A617453AEC82E7C8D);
		s_Il2CppMethodInitialized = true;
	}
	{
		int32_t L_0;
		L_0 = Shader_PropertyToID_mE98523D50F5656CAE89B30695C458253EB8956CA(_stringLiteral145EC6B8C2BA623D11BDD6955CB20C5B0C766445, NULL);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___ZenithId = L_0;
		int32_t L_1;
		L_1 = Shader_PropertyToID_mE98523D50F5656CAE89B30695C458253EB8956CA(_stringLiteral06EA5078C0497FD5E98F2AC8A4ACBBF3AE62DDBC, NULL);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___HorizonId = L_1;
		int32_t L_2;
		L_2 = Shader_PropertyToID_mE98523D50F5656CAE89B30695C458253EB8956CA(_stringLiteralBB816673BA1055A7FE7842C8E1DC06FAC9CCEA45, NULL);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___LimbId = L_2;
		int32_t L_3;
		L_3 = Shader_PropertyToID_mE98523D50F5656CAE89B30695C458253EB8956CA(_stringLiteralBD5B20F7F6017F0873E4EF984CB16D9DC6708E3E, NULL);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___GroundId = L_3;
		int32_t L_4;
		L_4 = Shader_PropertyToID_mE98523D50F5656CAE89B30695C458253EB8956CA(_stringLiteral59E9A90ACEB12BE1578CBAE545691B1979987312, NULL);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___SunColorId = L_4;
		int32_t L_5;
		L_5 = Shader_PropertyToID_mE98523D50F5656CAE89B30695C458253EB8956CA(_stringLiteral35C1F12C5BBBFC1B19520B74D959C91FA2FFD16D, NULL);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___SunDirId = L_5;
		int32_t L_6;
		L_6 = Shader_PropertyToID_mE98523D50F5656CAE89B30695C458253EB8956CA(_stringLiteralF873D6748545B7516CD56A7A617453AEC82E7C8D, NULL);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___ParamsId = L_6;
		int32_t L_7;
		L_7 = Shader_PropertyToID_mE98523D50F5656CAE89B30695C458253EB8956CA(_stringLiteralB9AEECB59BF43D9ACA81E7B9AEF765A7DB4BA263, NULL);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___MilkyId = L_7;
		int32_t L_8;
		L_8 = Shader_PropertyToID_mE98523D50F5656CAE89B30695C458253EB8956CA(_stringLiteral16571D1FE92CF04A8148F1652D0B353AD4471457, NULL);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___CloudAId = L_8;
		int32_t L_9;
		L_9 = Shader_PropertyToID_mE98523D50F5656CAE89B30695C458253EB8956CA(_stringLiteral8EC2A8CF93EE6CCEE33C770888DD7080CDAC60E1, NULL);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___CloudHId = L_9;
		int32_t L_10;
		L_10 = Shader_PropertyToID_mE98523D50F5656CAE89B30695C458253EB8956CA(_stringLiteral32EFDDFEAA75BD8C9B227E8BC9CCE4B7268C0716, NULL);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___WindId = L_10;
		int32_t L_11;
		L_11 = Shader_PropertyToID_mE98523D50F5656CAE89B30695C458253EB8956CA(_stringLiteral286F5A1B75EBF2E4DAD1784902DE57222F307786, NULL);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___Eq0Id = L_11;
		int32_t L_12;
		L_12 = Shader_PropertyToID_mE98523D50F5656CAE89B30695C458253EB8956CA(_stringLiteral682657B19B8755087EB65BB148A26CA596BDE3C9, NULL);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___Eq1Id = L_12;
		int32_t L_13;
		L_13 = Shader_PropertyToID_mE98523D50F5656CAE89B30695C458253EB8956CA(_stringLiteralF3C2A64A9C8933B4A8FBBDAFF40694249C19FAC9, NULL);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___Eq2Id = L_13;
		int32_t L_14;
		L_14 = Shader_PropertyToID_mE98523D50F5656CAE89B30695C458253EB8956CA(_stringLiteral6EEC04A3DFB247A18B195A7D12B42F30FFB94CAD, NULL);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___GalNId = L_14;
		int32_t L_15;
		L_15 = Shader_PropertyToID_mE98523D50F5656CAE89B30695C458253EB8956CA(_stringLiteralA6AF09A2719F5DE17063C132A6811819EF06AC36, NULL);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___StarTexId = L_15;
		int32_t L_16;
		L_16 = Shader_PropertyToID_mE98523D50F5656CAE89B30695C458253EB8956CA(_stringLiteral9C14B5BA0DE646558EC300FF192C1B945F94D345, NULL);
		((SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_StaticFields*)il2cpp_codegen_static_fields_for(SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0_il2cpp_TypeInfo_var))->___StarsId = L_16;
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
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* StarBake_Bake_m6581337B736F5EEB31BB6C44F7BB503EAD7A6DBE (RuntimeObject* ___0_stars, int32_t ___1_width, int32_t ___2_height, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IReadOnlyCollection_1_tF04B3A2A0FC144BE564160A18D95B937D9DB089E_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IReadOnlyList_1_t3DA86A967B1361A187E0FD611FC1F442964E6B58_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral9F2140104281DD17C0E174E25ACB6E50A862FA7F);
		s_Il2CppMethodInitialized = true;
	}
	ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* V_0 = NULL;
	int32_t V_1 = 0;
	StarPoint_t8DA928CE6AE24DC5827194FA6BD3B437EC92A3CE V_2;
	memset((&V_2), 0, sizeof(V_2));
	int32_t V_3 = 0;
	int32_t V_4 = 0;
	Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 V_5;
	memset((&V_5), 0, sizeof(V_5));
	float V_6 = 0.0f;
	{
		int32_t L_0 = ___1_width;
		if ((((int32_t)L_0) >= ((int32_t)8)))
		{
			goto IL_0007;
		}
	}
	{
		___1_width = 8;
	}

IL_0007:
	{
		int32_t L_1 = ___2_height;
		if ((((int32_t)L_1) >= ((int32_t)8)))
		{
			goto IL_000e;
		}
	}
	{
		___2_height = 8;
	}

IL_000e:
	{
		int32_t L_2 = ___1_width;
		int32_t L_3 = ___2_height;
		ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* L_4 = (ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389*)(ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389*)SZArrayNew(ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389_il2cpp_TypeInfo_var, (uint32_t)((int32_t)il2cpp_codegen_multiply(L_2, L_3)));
		V_0 = L_4;
		RuntimeObject* L_5 = ___0_stars;
		if (!L_5)
		{
			goto IL_007b;
		}
	}
	{
		RuntimeObject* L_6 = ___0_stars;
		NullCheck(L_6);
		int32_t L_7;
		L_7 = InterfaceFuncInvoker0< int32_t >::Invoke(0, IReadOnlyCollection_1_tF04B3A2A0FC144BE564160A18D95B937D9DB089E_il2cpp_TypeInfo_var, L_6);
		V_1 = ((int32_t)il2cpp_codegen_subtract(L_7, 1));
		goto IL_0077;
	}

IL_0025:
	{
		RuntimeObject* L_8 = ___0_stars;
		int32_t L_9 = V_1;
		NullCheck(L_8);
		StarPoint_t8DA928CE6AE24DC5827194FA6BD3B437EC92A3CE L_10;
		L_10 = InterfaceFuncInvoker1< StarPoint_t8DA928CE6AE24DC5827194FA6BD3B437EC92A3CE, int32_t >::Invoke(0, IReadOnlyList_1_t3DA86A967B1361A187E0FD611FC1F442964E6B58_il2cpp_TypeInfo_var, L_8, L_9);
		V_2 = L_10;
		StarPoint_t8DA928CE6AE24DC5827194FA6BD3B437EC92A3CE L_11 = V_2;
		double L_12 = L_11.___RaDeg;
		StarPoint_t8DA928CE6AE24DC5827194FA6BD3B437EC92A3CE L_13 = V_2;
		double L_14 = L_13.___DecDeg;
		int32_t L_15 = ___1_width;
		int32_t L_16 = ___2_height;
		SkyBodies_EquirectTexel_mBCBB527630A3EA4D30E588FBCB173690E300AF0B(L_12, L_14, L_15, L_16, (&V_3), (&V_4), NULL);
		StarPoint_t8DA928CE6AE24DC5827194FA6BD3B437EC92A3CE L_17 = V_2;
		double L_18 = L_17.___BV;
		Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_19;
		L_19 = SkyBodies_ColorFromBv_m1935C8E628927002C86EC08773C04EB5AFC627D0(L_18, NULL);
		V_5 = L_19;
		StarPoint_t8DA928CE6AE24DC5827194FA6BD3B437EC92A3CE L_20 = V_2;
		double L_21 = L_20.___VMag;
		double L_22;
		L_22 = SkyBodies_DisplayGain_mBF76B46A7AC5A713D1F3DB9C294B22135B459073(L_21, NULL);
		float L_23;
		L_23 = Mathf_Clamp01_mA7E048DBDA832D399A581BE4D6DED9FA44CE0F14_inline(((float)L_22), NULL);
		V_6 = L_23;
		ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* L_24 = V_0;
		int32_t L_25 = ___1_width;
		int32_t L_26 = ___2_height;
		int32_t L_27 = V_3;
		int32_t L_28 = V_4;
		Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_29 = V_5;
		float L_30 = V_6;
		StarBake_Stamp_m30ED1DF8B10E40D462BFE464F2A22E12F7E57416(L_24, L_25, L_26, L_27, L_28, L_29, L_30, NULL);
		int32_t L_31 = V_1;
		V_1 = ((int32_t)il2cpp_codegen_subtract(L_31, 1));
	}

IL_0077:
	{
		int32_t L_32 = V_1;
		if ((((int32_t)L_32) >= ((int32_t)0)))
		{
			goto IL_0025;
		}
	}

IL_007b:
	{
		int32_t L_33 = ___1_width;
		int32_t L_34 = ___2_height;
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_35 = (Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4*)il2cpp_codegen_object_new(Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4_il2cpp_TypeInfo_var);
		Texture2D__ctor_mECF60A9EC0638EC353C02C8E99B6B465D23BE917(L_35, L_33, L_34, 4, (bool)0, NULL);
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_36 = L_35;
		NullCheck(L_36);
		Object_set_name_mC79E6DC8FFD72479C90F0C4CC7F42A0FEAF5AE47(L_36, _stringLiteral9F2140104281DD17C0E174E25ACB6E50A862FA7F, NULL);
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_37 = L_36;
		NullCheck(L_37);
		Texture_set_wrapModeU_m2002CCB73143182032BD5ED6C80AB9A0ED2A0981(L_37, 0, NULL);
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_38 = L_37;
		NullCheck(L_38);
		Texture_set_wrapModeV_m3C79177BAE70103DDBF871896AF464513A0F919D(L_38, 1, NULL);
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_39 = L_38;
		NullCheck(L_39);
		Texture_set_filterMode_mE423E58C0C16D059EA62BA87AD70F44AEA50CCC9(L_39, 0, NULL);
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_40 = L_39;
		NullCheck(L_40);
		Texture_set_anisoLevel_m768759DE9D6BE15FCE995F5FC468980B904B9D1F(L_40, 0, NULL);
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_41 = L_40;
		NullCheck(L_41);
		Object_set_hideFlags_mACB8BFC903FB3B01BBD427753E791BF28B5E33D4(L_41, ((int32_t)61), NULL);
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_42 = L_41;
		ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* L_43 = V_0;
		NullCheck(L_42);
		Texture2D_SetPixels_mAE0CDFA15FA96F840D7FFADC31405D8AF20D9073(L_42, L_43, NULL);
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_44 = L_42;
		NullCheck(L_44);
		Texture2D_Apply_m36EE27E6F1BF7FB8C70A1D749DC4EE249810AA3A(L_44, (bool)0, (bool)1, NULL);
		return L_44;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void StarBake_Stamp_m30ED1DF8B10E40D462BFE464F2A22E12F7E57416 (ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* ___0_px, int32_t ___1_w, int32_t ___2_h, int32_t ___3_cx, int32_t ___4_cy, Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 ___5_col, float ___6_gain, const RuntimeMethod* method) 
{
	int32_t V_0 = 0;
	int32_t V_1 = 0;
	{
		int32_t L_0 = ___4_cy;
		if ((((int32_t)L_0) < ((int32_t)0)))
		{
			goto IL_000a;
		}
	}
	{
		int32_t L_1 = ___4_cy;
		int32_t L_2 = ___2_h;
		if ((((int32_t)L_1) < ((int32_t)L_2)))
		{
			goto IL_000b;
		}
	}

IL_000a:
	{
		return;
	}

IL_000b:
	{
		int32_t L_3 = ___3_cx;
		int32_t L_4 = ___1_w;
		V_0 = ((int32_t)(L_3%L_4));
		int32_t L_5 = V_0;
		if ((((int32_t)L_5) >= ((int32_t)0)))
		{
			goto IL_0017;
		}
	}
	{
		int32_t L_6 = V_0;
		int32_t L_7 = ___1_w;
		V_0 = ((int32_t)il2cpp_codegen_add(L_6, L_7));
	}

IL_0017:
	{
		int32_t L_8 = ___4_cy;
		int32_t L_9 = ___1_w;
		int32_t L_10 = V_0;
		V_1 = ((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(L_8, L_9)), L_10));
		ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* L_11 = ___0_px;
		int32_t L_12 = V_1;
		NullCheck(L_11);
		float L_13 = ((L_11)->GetAddressAt(static_cast<il2cpp_array_size_t>(L_12)))->___a;
		float L_14 = ___6_gain;
		if ((!(((float)L_13) >= ((float)L_14))))
		{
			goto IL_002f;
		}
	}
	{
		return;
	}

IL_002f:
	{
		ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* L_15 = ___0_px;
		int32_t L_16 = V_1;
		Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_17 = ___5_col;
		double L_18 = L_17.___R;
		float L_19 = ___6_gain;
		Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_20 = ___5_col;
		double L_21 = L_20.___G;
		float L_22 = ___6_gain;
		Rgb_tEF6D4B3CE65E3A38E88BE8766A7922DF6D5B2398 L_23 = ___5_col;
		double L_24 = L_23.___B;
		float L_25 = ___6_gain;
		float L_26 = ___6_gain;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_27;
		memset((&L_27), 0, sizeof(L_27));
		Color__ctor_m3786F0D6E510D9CFA544523A955870BD2A514C8C_inline((&L_27), ((float)il2cpp_codegen_multiply(((float)L_18), L_19)), ((float)il2cpp_codegen_multiply(((float)L_21), L_22)), ((float)il2cpp_codegen_multiply(((float)L_24), L_25)), L_26, NULL);
		NullCheck(L_15);
		(L_15)->SetAt(static_cast<il2cpp_array_size_t>(L_16), (Color_tD001788D726C3A7F1379BEED0260B9591F440C1F)L_27);
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
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float SkySun_get_ElevationDeg_m88C47C05DAB5E5D03500C5F8E53F677A31E17113_inline (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&SkySun_t55412B2FF59478EDEEE01F79F3B0E738516396BD_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		il2cpp_codegen_runtime_class_init_inline(SkySun_t55412B2FF59478EDEEE01F79F3B0E738516396BD_il2cpp_TypeInfo_var);
		float L_0 = ((SkySun_t55412B2FF59478EDEEE01F79F3B0E738516396BD_StaticFields*)il2cpp_codegen_static_fields_for(SkySun_t55412B2FF59478EDEEE01F79F3B0E738516396BD_il2cpp_TypeInfo_var))->___U3CElevationDegU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void SkyRig_set_Grade_m2DFB8271381DE1BAF1321B6B0E623E833031C8C0_inline (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C ___0_value, const RuntimeMethod* method) 
{
	{
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_0 = ___0_value;
		__this->___U3CGradeU3Ek__BackingField = L_0;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C SkyRig_get_Grade_mB922C963C06C2B47481FCC64085AE25BA134C453_inline (SkyRig_tCCA842A804A3DFA28378D964974BE815698128A0* __this, const RuntimeMethod* method) 
{
	{
		SkyGrade_tE0809C3434E5DA69F1421AE9FE432139AD22641C L_0 = __this->___U3CGradeU3Ek__BackingField;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Vector4__ctor_m96B2CD8B862B271F513AF0BDC2EABD58E4DBC813_inline (Vector4_t58B63D32F48C0DBF50DE2C60794C4676C80EDBE3* __this, float ___0_x, float ___1_y, float ___2_z, float ___3_w, const RuntimeMethod* method) 
{
	{
		float L_0 = ___0_x;
		__this->___x = L_0;
		float L_1 = ___1_y;
		__this->___y = L_1;
		float L_2 = ___2_z;
		__this->___z = L_2;
		float L_3 = ___3_w;
		__this->___w = L_3;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Mathf_Lerp_m47EF2FFB7647BD0A1FDC26DC03E28B19812139B5_inline (float ___0_a, float ___1_b, float ___2_t, const RuntimeMethod* method) 
{
	float V_0 = 0.0f;
	{
		float L_0 = ___0_a;
		float L_1 = ___1_b;
		float L_2 = ___0_a;
		float L_3 = ___2_t;
		float L_4;
		L_4 = Mathf_Clamp01_mA7E048DBDA832D399A581BE4D6DED9FA44CE0F14_inline(L_3, NULL);
		V_0 = ((float)il2cpp_codegen_add(L_0, ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_subtract(L_1, L_2)), L_4))));
		goto IL_0010;
	}

IL_0010:
	{
		float L_5 = V_0;
		return L_5;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Vector3__ctor_m376936E6B999EF1ECBE57D990A386303E2283DE0_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* __this, float ___0_x, float ___1_y, float ___2_z, const RuntimeMethod* method) 
{
	{
		float L_0 = ___0_x;
		__this->___x = L_0;
		float L_1 = ___1_y;
		__this->___y = L_1;
		float L_2 = ___2_z;
		__this->___z = L_2;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Vector3_get_sqrMagnitude_m43C27DEC47C4811FB30AB474FF2131A963B66FC8_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* __this, const RuntimeMethod* method) 
{
	{
		float L_0 = __this->___x;
		float L_1 = __this->___x;
		float L_2 = __this->___y;
		float L_3 = __this->___y;
		float L_4 = __this->___z;
		float L_5 = __this->___z;
		return ((float)il2cpp_codegen_add(((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(L_0, L_1)), ((float)il2cpp_codegen_multiply(L_2, L_3)))), ((float)il2cpp_codegen_multiply(L_4, L_5))));
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_get_up_m128AF3FDC820BF59D5DE86D973E7DE3F20C3AEBA_inline (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_0 = ((Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_StaticFields*)il2cpp_codegen_static_fields_for(Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_il2cpp_TypeInfo_var))->___upVector;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_get_normalized_m736BBF65D5CDA7A18414370D15B4DFCC1E466F07_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* __this, const RuntimeMethod* method) 
{
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_0;
		L_0 = Vector3_Normalize_m1DE89849FE2019B6AA0031306896F3B800849399_inline(__this, NULL);
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_op_UnaryNegation_m5450829F333BD2A88AF9A592C4EE331661225915_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___0_a, const RuntimeMethod* method) 
{
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_0;
	memset((&V_0), 0, sizeof(V_0));
	{
		il2cpp_codegen_initobj((&V_0), sizeof(Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2));
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_0 = ___0_a;
		float L_1 = L_0.___x;
		(&V_0)->___x = ((-L_1));
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_2 = ___0_a;
		float L_3 = L_2.___y;
		(&V_0)->___y = ((-L_3));
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_4 = ___0_a;
		float L_5 = L_4.___z;
		(&V_0)->___z = ((-L_5));
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_6 = V_0;
		return L_6;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 Quaternion_LookRotation_mDB2CCA75B8E1AB98104F2A6E1A1EA57D0D1298D7_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___0_forward, const RuntimeMethod* method) 
{
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_0;
	memset((&V_0), 0, sizeof(V_0));
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_0;
		L_0 = Vector3_get_up_m128AF3FDC820BF59D5DE86D973E7DE3F20C3AEBA_inline(NULL);
		V_0 = L_0;
		Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 L_1;
		L_1 = Quaternion_Internal_LookRotation_m489E6A5E916949FFE91C0B11A60C6016D996C1E0((&___0_forward), (&V_0), NULL);
		return L_1;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Color__ctor_m3786F0D6E510D9CFA544523A955870BD2A514C8C_inline (Color_tD001788D726C3A7F1379BEED0260B9591F440C1F* __this, float ___0_r, float ___1_g, float ___2_b, float ___3_a, const RuntimeMethod* method) 
{
	{
		float L_0 = ___0_r;
		__this->___r = L_0;
		float L_1 = ___1_g;
		__this->___g = L_1;
		float L_2 = ___2_b;
		__this->___b = L_2;
		float L_3 = ___3_a;
		__this->___a = L_3;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Color_tD001788D726C3A7F1379BEED0260B9591F440C1F Color_get_black_mB50217951591A045844C61E7FF31EEE3FEF16737_inline (const RuntimeMethod* method) 
{
	{
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_0;
		memset((&L_0), 0, sizeof(L_0));
		Color__ctor_m3786F0D6E510D9CFA544523A955870BD2A514C8C_inline((&L_0), (0.0f), (0.0f), (0.0f), (1.0f), NULL);
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Mathf_Clamp01_mA7E048DBDA832D399A581BE4D6DED9FA44CE0F14_inline (float ___0_value, const RuntimeMethod* method) 
{
	bool V_0 = false;
	float V_1 = 0.0f;
	bool V_2 = false;
	{
		float L_0 = ___0_value;
		V_0 = (bool)((((float)L_0) < ((float)(0.0f)))? 1 : 0);
		bool L_1 = V_0;
		if (!L_1)
		{
			goto IL_0015;
		}
	}
	{
		V_1 = (0.0f);
		goto IL_002d;
	}

IL_0015:
	{
		float L_2 = ___0_value;
		V_2 = (bool)((((float)L_2) > ((float)(1.0f)))? 1 : 0);
		bool L_3 = V_2;
		if (!L_3)
		{
			goto IL_0029;
		}
	}
	{
		V_1 = (1.0f);
		goto IL_002d;
	}

IL_0029:
	{
		float L_4 = ___0_value;
		V_1 = L_4;
		goto IL_002d;
	}

IL_002d:
	{
		float L_5 = V_1;
		return L_5;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t List_1_get_Count_m5BA8B6A985C4ECE8C3EED6F7425B52AAB9BF2870_gshared_inline (List_1_t1DEA6CD9D61565F706A372E5DA1D6E3DF31064BE* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = __this->____size;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_Normalize_m1DE89849FE2019B6AA0031306896F3B800849399_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* ___0_value, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_1;
	memset((&V_1), 0, sizeof(V_1));
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_2;
	memset((&V_2), 0, sizeof(V_2));
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 G_B3_0;
	memset((&G_B3_0), 0, sizeof(G_B3_0));
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* L_0 = ___0_value;
		float L_1;
		L_1 = Vector3_get_magnitude_mF0D6017E90B345F1F52D1CC564C640F1A847AF2D_inline(L_0, NULL);
		V_0 = L_1;
		float L_2 = V_0;
		if ((((float)L_2) > ((float)(9.99999975E-06f))))
		{
			goto IL_0017;
		}
	}
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_3 = ((Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_StaticFields*)il2cpp_codegen_static_fields_for(Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_il2cpp_TypeInfo_var))->___zeroVector;
		G_B3_0 = L_3;
		goto IL_004d;
	}

IL_0017:
	{
		il2cpp_codegen_initobj((&V_1), sizeof(Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2));
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* L_4 = ___0_value;
		float L_5 = L_4->___x;
		float L_6 = V_0;
		(&V_1)->___x = ((float)(L_5/L_6));
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* L_7 = ___0_value;
		float L_8 = L_7->___y;
		float L_9 = V_0;
		(&V_1)->___y = ((float)(L_8/L_9));
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* L_10 = ___0_value;
		float L_11 = L_10->___z;
		float L_12 = V_0;
		(&V_1)->___z = ((float)(L_11/L_12));
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_13 = V_1;
		G_B3_0 = L_13;
	}

IL_004d:
	{
		V_2 = G_B3_0;
		goto IL_0050;
	}

IL_0050:
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_14 = V_2;
		return L_14;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Vector3_get_magnitude_mF0D6017E90B345F1F52D1CC564C640F1A847AF2D_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		float L_0 = __this->___x;
		float L_1 = __this->___x;
		float L_2 = __this->___y;
		float L_3 = __this->___y;
		float L_4 = __this->___z;
		float L_5 = __this->___z;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_6;
		L_6 = sqrt(((double)((float)il2cpp_codegen_add(((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(L_0, L_1)), ((float)il2cpp_codegen_multiply(L_2, L_3)))), ((float)il2cpp_codegen_multiply(L_4, L_5))))));
		return ((float)L_6);
	}
}
