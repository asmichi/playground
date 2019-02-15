_DATA SEGMENT
_DATA ENDS
_TEXT SEGMENT

; choose one
BY_LEA1 equ 1
;BY_LEA2 equ 1
;BY_SHL equ 1
;BY_MUL equ 1
;BY_ADD equ 1

; sum(int count, Fuga* p1, Fuga* p2)
PUBLIC sum
sum PROC
	push rbx
	push rdx
	push rsi
	push rdi
	push r12
	push r13

	mov rsi, rdx
	mov rdi, r8
	mov edx, ecx
	mov rcx, 0
L1:

IFDEF BY_LEA1
	lea rbx, [rcx * 8]
	add r12, [rsi + rbx * 8]
ELSEIFDEF BY_LEA2
	lea rbx, [rcx * 8]
	lea rbx, [rbx * 8]
	add r12, [rsi + rbx]
ELSEIFDEF BY_SHL
	mov rbx, rcx
	shl rbx, 6
	add r12, [rsi + rbx]
ELSEIFDEF BY_MUL 
	imul rbx, rcx, 64
	add r12, [rsi + rbx]
ELSEIFDEF BY_ADD
	add r12, [rsi + rbx]
ENDIF

IFDEF BY_LEA1
	lea rbx, [rcx * 8]
	add r13, [rdi + rbx * 8]
ELSEIFDEF BY_LEA2
	lea rbx, [rcx * 8]
	lea rbx, [rbx * 8]
	add r13, [rdi + rbx]
ELSEIFDEF BY_SHL
	mov rbx, rcx
	shl rbx, 6
	add r13, [rdi + rbx]
ELSEIFDEF BY_MUL
	imul rbx, rcx, 64
	add r13, [rdi + rbx]
ELSEIFDEF BY_ADD
	add r13, [rdi + rbx]
ENDIF

	add rcx, 1
IFDEF BY_ADD
	add rbx, 64
ENDIF
	cmp rcx, rdx
	jne L1

	mov rax, r12
	add rax, r13
	pop r13
	pop r12
	pop rdi
	pop rsi
	pop rdx
	pop rbx
	ret
sum ENDP

_TEXT ENDS
END