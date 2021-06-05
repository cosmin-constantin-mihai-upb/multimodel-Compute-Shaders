#version 310 es

layout(local_size_x = 1024) in;

struct ShaderSystemBuffer
{
	float K1, K2, K3;			   //pid coeficients
	float R;					   //reference
	float u0, u1, u2, u3;		   //past commands
	float e0, e1, e2, e3;		   //input errors of real system
	float y0, y1, y2, y3;		   //past simulated outputs
	float m;					   //robustness coeficient
	float P, I, D;				   //P I and D of controller
	float up0, up1, up2;		   //previous commands issued by real controller
	float ykn;					   //current output of real process
	float es0, es1, es2, es3;	   //output errors of ys
	float c0, c1, c2, c3, c4;	   //identified model coeficients 
	float cp0, cp1, cp2, cp3, cp4; //identified process coeficients
	float DeltaY, DY1, DY2, DY3;   //difference between real system output and yk
	float us0, us1, us2, us3;	   //past simulated commands
	float plotOutput;
	float yknAverage;
	float yk0, yk1, yk2, yk3;	   //past outputs of real controller + model
	float ek0, ek1, ek2, ek3;	   // output errors of yk
	float yknAvg;				   //average of real system outputs
	float SwitchingCriterion;
};

layout(std430, binding = 3) buffer SystemState {
	ShaderSystemBuffer data[];		// This array can now be tightly packed.
}input_data;


void main()
{
	uint ident = gl_GlobalInvocationID.x;
	//get the PID command

	//compute the command using the controller parameters and the real system output errors

	float u0 = input_data.data[ident].up0 +
		input_data.data[ident].K1 * input_data.data[ident].e0 +
		input_data.data[ident].K2 * input_data.data[ident].e1 +
		input_data.data[ident].K3 * input_data.data[ident].e2;

	float us0 = input_data.data[ident].us0 +
		input_data.data[ident].K1 * input_data.data[ident].es0 +
		input_data.data[ident].K2 * input_data.data[ident].es1 +
		input_data.data[ident].K3 * input_data.data[ident].es2;


	input_data.data[ident].u3 = input_data.data[ident].u2;
	input_data.data[ident].u2 = input_data.data[ident].u1;
	input_data.data[ident].u1 = input_data.data[ident].u0;
	input_data.data[ident].u0 = u0;

	/*compute model response with simulated controller with real system output errors*/
	float ys = (input_data.data[ident].c4 * input_data.data[ident].u2) +
		(input_data.data[ident].c3 * input_data.data[ident].u1) +
		(input_data.data[ident].c2 * input_data.data[ident].u0) +
		(input_data.data[ident].c1 * input_data.data[ident].yk1) +
		(input_data.data[ident].c0 * input_data.data[ident].yk0);

	input_data.data[ident].y3 = input_data.data[ident].y2;
	input_data.data[ident].y2 = input_data.data[ident].y1;
	input_data.data[ident].y1 = input_data.data[ident].y0;
	input_data.data[ident].y0 = ys;

	/*compute model response with real controller*/
	float yk = (input_data.data[ident].c4 * input_data.data[ident].up2) +
		(input_data.data[ident].c3 * input_data.data[ident].up1) +
		(input_data.data[ident].c2 * input_data.data[ident].up0) +
		(input_data.data[ident].c1 * input_data.data[ident].yk1) +
		(input_data.data[ident].c0 * input_data.data[ident].yk0);

	float ykn = input_data.data[ident].ykn;

	input_data.data[ident].yk3 = input_data.data[ident].yk2;
	input_data.data[ident].yk2 = input_data.data[ident].yk1;
	input_data.data[ident].yk1 = input_data.data[ident].yk0;
	input_data.data[ident].yk0 = yk;

	input_data.data[ident].plotOutput = yk;

	input_data.data[ident].ek3 = input_data.data[ident].ek2;
	input_data.data[ident].ek2 = input_data.data[ident].ek1;
	input_data.data[ident].ek1 = input_data.data[ident].ek0;
	input_data.data[ident].ek0 = input_data.data[ident].R - yk;

	input_data.data[ident].es3 = input_data.data[ident].es2;
	input_data.data[ident].es2 = input_data.data[ident].es1;
	input_data.data[ident].es1 = input_data.data[ident].es0;
	input_data.data[ident].es0 = input_data.data[ident].R - ys;

	input_data.data[ident].us3 = input_data.data[ident].us2;
	input_data.data[ident].us2 = input_data.data[ident].us1;
	input_data.data[ident].us1 = input_data.data[ident].us0;
	input_data.data[ident].us0 = us0;

	input_data.data[ident].DY3 = input_data.data[ident].DY2;
	input_data.data[ident].DY2 = input_data.data[ident].DY1;
	input_data.data[ident].DY1 = input_data.data[ident].DeltaY;
	input_data.data[ident].DeltaY = abs(input_data.data[ident].ykn - yk);

	//performance margins
	float oneykn = 1.0 + input_data.data[ident].ykn;
	float oneyk = 1.0 + ys;
	input_data.data[ident].m = abs((ykn / oneykn) - (yk / oneyk));

	input_data.data[ident].yknAverage = (input_data.data[ident].y0 +
		input_data.data[ident].y1 +
		input_data.data[ident].y2 +
		input_data.data[ident].y3) / 4.0;

	float alpha = 1.0;
	float beta = 0.0;
	input_data.data[ident].SwitchingCriterion = alpha * input_data.data[ident].DeltaY +
		beta * (input_data.data[ident].DY1 + input_data.data[ident].DY2 + input_data.data[ident].DY3);
}
