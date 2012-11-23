/* Wrapper for Mosek QP solver */
/* 6 November 2007 */

#include "stdafx.h"

#include <mosek.h>

#include <cstdio>
#include <cassert>

const int MosekExitErrorCode = 5;

void MSKAPI MosekLogHandler(void *handle, char str[]) {
	printf("%s\n", str);
}

void OnMosekOpFailure(MSKrescode_enum opCode) {
	char symname[MSK_MAX_STR_LEN];
	char desc[MSK_MAX_STR_LEN];
        
	MSK_getcodedesc(opCode, symname, desc);
	fprintf(stderr, "MOSEK error %s: %s\n", symname, desc);

	assert(0 && "Mosek error");
	exit(MosekExitErrorCode);
}

#define MOSEK_CHECK(x) { MSKrescode_enum res = x; if (res != MSK_RES_OK) OnMosekOpFailure(res); }

void mosek_qp_optimize(double** G, double* delta, double* alpha, long k, double C, double *dual_obj, long nnK) {
	long i,j,t;
	double *c;
	MSKlidxt *aptrb;
	MSKlidxt *aptre;
	MSKidxt *asub;
	double *aval;
	MSKboundkeye bkc[1];
	double blc[1];
	double buc[1];
	MSKboundkeye *bkx;
	double *blx;
	double *bux;
	MSKidxt *qsubi,*qsubj;
	double *qval;

	MSKenv_t env;
	MSKtask_t task;

	c = (double*) malloc(sizeof(double)*k);
	assert(c!=NULL);
	aptrb = (MSKlidxt*) malloc(sizeof(MSKlidxt)*(k));
	assert(aptrb!=NULL);
	aptre = (MSKlidxt*) malloc(sizeof(MSKlidxt)*(k));
	assert(aptre!=NULL);
	asub = (MSKidxt*) malloc(sizeof(MSKidxt)*(k - nnK));
	assert(asub!=NULL);
	aval = (double*) malloc(sizeof(double)*(k - nnK));
	assert(aval!=NULL);
	bkx = (MSKboundkeye*) malloc(sizeof(MSKboundkeye)*k);
	assert(bkx!=NULL);
	blx = (double*) malloc(sizeof(double)*k);
	assert(blx!=NULL);
	bux = (double*) malloc(sizeof(double)*k);
	assert(bux!=NULL);
	qsubi = (MSKidxt*) malloc(sizeof(MSKidxt)*(k*(k+1)/2));
	assert(qsubi!=NULL);  
	qsubj = (MSKidxt*) malloc(sizeof(MSKidxt)*(k*(k+1)/2));
	assert(qsubj!=NULL);  
	qval = (double*) malloc(sizeof(double)*(k*(k+1)/2));
	assert(qval!=NULL);  

	for (i=0;i<k;i++) {
		aptrb[i] = i >= nnK ? i - nnK : 0;
		aptre[i] = i >= nnK ? i - nnK + 1 : 0;
		if (i >= nnK) {
			asub[i - nnK] = 0;
			aval[i - nnK] = 1.0;
		}

		c[i] = delta[i];

		bkx[i] = MSK_BK_LO;
		blx[i] = 0.0;
		bux[i] = MSK_INFINITY;
	}

	/*
	bkc[0] = MSK_BK_UP;
	blc[0] = -MSK_INFINITY;
	buc[0] = C;
	*/
	bkc[0] = MSK_BK_FX;
	blc[0] = C;
	buc[0] = C;  

	/* coefficients for the Gram matrix */
	t = 0;
	for (i=0;i<k;i++) {
		for (j=0;j<=i;j++) {
			qsubi[t] = i;
			qsubj[t] = j;
			qval[t] = G[i][j];
			t++;
		}
	}
  
	// create mosek environment
	MOSEK_CHECK(MSK_makeenv(&env, NULL, NULL, NULL, NULL));

	// setup environment
	MOSEK_CHECK(MSK_linkfunctoenvstream(env, MSK_STREAM_ERR, NULL, MosekLogHandler));
	MOSEK_CHECK(MSK_linkfunctoenvstream(env, MSK_STREAM_WRN, NULL, MosekLogHandler));

	// initialize the environment
	MOSEK_CHECK(MSK_initenv(env));

	// create the optimization task
	MOSEK_CHECK(MSK_maketask(env,1,k,&task));
	
	// setup the optimization task
	MOSEK_CHECK(MSK_linkfunctotaskstream(task, MSK_STREAM_ERR, NULL, MosekLogHandler));
	MOSEK_CHECK(MSK_linkfunctotaskstream(task, MSK_STREAM_WRN, NULL, MosekLogHandler));
	MOSEK_CHECK(MSK_putdouparam(task, MSK_DPAR_INTPNT_TOL_REL_GAP, 1E-14));
	MOSEK_CHECK(MSK_putintparam(task, MSK_IPAR_CHECK_CONVEXITY, MSK_CHECK_CONVEXITY_NONE));
	  
	// input linear part of data
	MOSEK_CHECK(MSK_inputdata(task,
				1,k,
				1,k,
				c,0.0,
				aptrb,aptre,
				asub,aval,
				bkc,blc,buc,
				bkx,blx,bux));
	    
	// input quadratic part of data
	MOSEK_CHECK(MSK_putqobj(task, k*(k+1)/2, qsubi, qsubj, qval));
	
	// run optimizer
	MOSEK_CHECK(MSK_optimize(task));
      
	// extract solution
	MOSEK_CHECK(MSK_getsolutionslice(task,
					MSK_SOL_ITR,
					MSK_SOL_ITEM_XX,
					0,
					k,
					alpha));
        
	// get primal obj
	MOSEK_CHECK(MSK_getprimalobj(task, MSK_SOL_ITR, dual_obj));
	
	// mosek cleanup
	MOSEK_CHECK(MSK_deletetask(&task));
	MOSEK_CHECK(MSK_deleteenv(&env));
  
	// free the memory
	free(c);
	free(aptrb);
	free(aptre);
	free(asub);
	free(aval);
	free(bkx);
	free(blx);
	free(bux);
	free(qsubi);  
	free(qsubj);  
	free(qval);  
}

