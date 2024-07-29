#include "Engine.h"

#include "codegen/il2cpp-codegen.h"

#include "Interpreter.h"
#include "MemoryUtil.h"
#include "../metadata/InterpreterImage.h"
#include "../metadata/MetadataModule.h"

namespace hybridclr
{
namespace interpreter
{
	InterpFrame* InterpFrameGroup::EnterFrameFromInterpreter(const MethodInfo* method, StackObject* argBase)
	{
#if IL2CPP_ENABLE_PROFILER
		il2cpp_codegen_profiler_method_enter(method);
#endif
		const InterpMethodInfo* imi = (const InterpMethodInfo*)method->interpData;
		int32_t oldStackTop = _machineState.GetStackTop();
		StackObject* stackBasePtr = _machineState.AllocStackSlot(imi->maxStackSize - imi->argStackObjectSize);
		InterpFrame* newFrame = _machineState.PushFrame();
		*newFrame = { method, argBase, oldStackTop, nullptr, nullptr, nullptr, 0, 0, _machineState.GetLocalPoolBottomIdx() };
		PUSH_STACK_FRAME(method);
		return newFrame;
	}


	InterpFrame* InterpFrameGroup::EnterFrameFromNative(const MethodInfo* method, StackObject* argBase)
	{
#if IL2CPP_ENABLE_PROFILER
		il2cpp_codegen_profiler_method_enter(method);
#endif
		const InterpMethodInfo* imi = (const InterpMethodInfo*)method->interpData;
		int32_t oldStackTop = _machineState.GetStackTop();
		StackObject* stackBasePtr = _machineState.AllocStackSlot(imi->maxStackSize);
		InterpFrame* newFrame = _machineState.PushFrame();
		*newFrame = { method, stackBasePtr, oldStackTop, nullptr, nullptr, nullptr, 0, 0, _machineState.GetLocalPoolBottomIdx() };

		// if not prepare arg stack. copy from args
		if (imi->args)
		{
			IL2CPP_ASSERT(imi->argCount == metadata::GetActualArgumentNum(method));
			CopyStackObject(stackBasePtr, argBase, imi->argStackObjectSize);
		}
		PUSH_STACK_FRAME(method);
		return newFrame;
	}

	InterpFrame* InterpFrameGroup::LeaveFrame()
	{
		IL2CPP_ASSERT(_machineState.GetFrameTopIdx() > _frameBaseIdx);
		POP_STACK_FRAME();
		InterpFrame* frame = _machineState.GetTopFrame();
#if IL2CPP_ENABLE_PROFILER
		il2cpp_codegen_profiler_method_exit(frame->method);
#endif
		if (frame->exFlowBase)
		{
			_machineState.SetExceptionFlowTop(frame->exFlowBase);
		}
		_machineState.PopFrame();
		_machineState.SetStackTop(frame->oldStackTop);
		_machineState.SetLocalPoolBottomIdx(frame->oldLocalPoolBottomIdx);
		return _machineState.GetFrameTopIdx() > _frameBaseIdx ? _machineState.GetTopFrame() : nullptr;
	}

	static bool FrameNeedsSkipped(const Il2CppStackFrameInfo& frame)
	{
		const MethodInfo* method = frame.method;
		const Il2CppClass* klass = method->klass;
		return (strcmp(klass->namespaze, "System.Diagnostics") == 0 &&
			(strcmp(klass->name, "StackFrame") == 0 || strcmp(klass->name, "StackTrace") == 0))
			|| (strcmp(klass->namespaze, "UnityEngine") == 0
				&& (strcmp(klass->name, "StackTraceUtility") == 0
					|| strcmp(klass->name, "Debug") == 0
					|| strcmp(klass->name, "Logger") == 0
					|| strcmp(klass->name, "DebugLogHandler") == 0));
	}

	static void SetupStackFrameInfo(const InterpFrame* frame, Il2CppStackFrameInfo& stackFrame)
	{
		const MethodInfo* method = frame->method;
		const InterpMethodInfo* imi = (const InterpMethodInfo*)method->interpData;
		const byte* actualIp;
		if (frame->ip >= imi->codes && frame->ip < imi->codes + imi->codeLength)
		{
			actualIp = frame->ip;
		}
		else
		{
			actualIp = *(byte**)frame->ip;
			IL2CPP_ASSERT(actualIp >= imi->codes && actualIp < imi->codes + imi->codeLength);
		}

		stackFrame.method = method;
#if HYBRIDCLR_UNITY_2020_OR_NEW
		stackFrame.raw_ip = (uintptr_t)actualIp;
#endif

		if (!hybridclr::metadata::IsInterpreterMethod(method))
		{
			return;
		}
		
		hybridclr::metadata::InterpreterImage* interpImage = hybridclr::metadata::MetadataModule::GetImage(method);
		if (!interpImage)
		{
			return;
		}

		hybridclr::metadata::PDBImage* pdbImage = interpImage->GetPDBImage();
		if (!pdbImage)
		{
			return;
		}
		pdbImage->SetupStackFrameInfo(method, actualIp, stackFrame);
	}

	void MachineState::CollectFrames(il2cpp::vm::StackFrames* stackFrames)
	{
		if (_frameTopIdx <= 0)
		{
			return;
		}
		size_t insertIndex = 0;
		for (; insertIndex < stackFrames->size(); insertIndex++)
		{
			if (FrameNeedsSkipped((*stackFrames)[insertIndex]))
			{
				break;
			}
		}
		stackFrames->insert(stackFrames->begin() + insertIndex, _frameTopIdx, Il2CppStackFrameInfo());
		for (int32_t i = 0; i < _frameTopIdx; i++)
		{
			SetupStackFrameInfo(_frameBase + i, (*stackFrames)[insertIndex + i]);
		}
	}
}
}

