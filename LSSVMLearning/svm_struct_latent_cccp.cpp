/* linear structural SVM with latent variables */
/* 30 September 2008 */

#include "stdafx.h"

#include "LearningTracker.h"

#include "svm_struct_latent_api.h"

#include <cstdio>
#include <cassert>
#include <cmath>

#define ALPHA_THRESHOLD 1E-14
//#define IDLE_ITER 20
//#define CLEANUP_CHECK 100
#define CUTTING_PLANE_EPS 1E-3

#define MAX_INNER_ITER_NO_VIOLATION 5
//#define MIN_OUTER_ITER 3
#define MAX_OUTER_ITER 10

#define MAX(x,y) ((x) < (y) ? (y) : (x))
#define MIN(x,y) ((x) > (y) ? (y) : (x))

#define DEBUG_LEVEL 1

/* mosek interface */
void mosek_qp_optimize(double** G, double* delta, double* alpha, long k, double C, double *dual_obj, long nnK);

void my_read_input_parameters(int argc, char* argv[], char *trainfile, char *modelfile,
			      LEARN_PARM *learn_parm, KERNEL_PARM *kernel_parm, STRUCT_LEARN_PARM *struct_parm);

void my_wait_any_key();

int resize_cleanup(int size_active, int *idle, double *alpha, double *delta, double *gammaG0, double *proximal_rhs, double **G, DOC **dXc, double *cut_error);

double sprod_nn(double *a, double *b, long n) {
  double ans=0.0;
  long i;
  for (i=1;i<n+1;i++) {
    ans+=a[i]*b[i];
  }
  return(ans);
}

void add_vector_nn(double *w, double *dense_x, long n, double factor) {
  long i;
  for (i=1;i<n+1;i++) {
    w[i]+=factor*dense_x[i];
  }
}

double* add_list_nn(SVECTOR *a, long totwords) 
     /* computes the linear combination of the SVECTOR list weighted
	by the factor of each SVECTOR. assumes that the number of
	features is small compared to the number of elements in the
	list */
{
    SVECTOR *f;
    long i;
    double *sum;

    sum=create_nvector(totwords);

    for(i=0;i<=totwords;i++) 
      sum[i]=0;

    for(f=a;f;f=f->next)  
      add_vector_ns(sum,f,f->factor);

    return(sum);
}


SVECTOR* find_cutting_plane(EXAMPLE *ex, SVECTOR **fycache, double *margin, long m, STRUCTMODEL *sm, STRUCT_LEARN_PARM *sparm) {

  long i;
  SVECTOR *f, *fy, *fybar, *lhs;
  LABEL       ybar;
  LATENT_VAR hbar;
  double lossval;
  double *new_constraint;

  long l,k;
  SVECTOR *fvec;
  WORD *words;  

  /* find cutting plane */
  lhs = NULL;
  *margin = 0;
  for (i=0;i<m;i++) {
    find_most_violated_constraint_marginrescaling(ex[i].x, ex[i].y, &ybar, &hbar, sm, sparm);
    /* get difference vector */
    fy = copy_svector(fycache[i]);
    fybar = psi(ex[i].x,ybar,hbar,sm,sparm);
    lossval = loss(ex[i].x,ex[i].y,ybar,hbar,sparm);
    free_label(ybar);
    free_latent_var(hbar);

	printf("psi=");
	for (int j = 0; j < sm->sizePsi; ++j)
		printf("%.4lf ", fybar->words[j].weight);
	printf("\n");

    /* scale difference vector */
    for (f=fy;f;f=f->next) {
      f->factor*=1.0/m;
      //f->factor*=ex[i].x.example_cost/m;
    }
    for (f=fybar;f;f=f->next) {
      f->factor*=-1.0/m;
      //f->factor*=-ex[i].x.example_cost/m;
    }
    /* add ybar to constraint */
    append_svector_list(fy,lhs);
    append_svector_list(fybar,fy);
    lhs = fybar;
    *margin+=lossval/m;
    //*margin+=lossval*ex[i].x.example_cost/m;
  }
  /* compact the linear representation */
  new_constraint = add_list_nn(lhs, sm->sizePsi);
  free_svector(lhs);

  /* DEBUG */
  printf("new_constraint=");
  for (i=1;i<sm->sizePsi+1;i++)
	  printf("%.4lf ", new_constraint[i]);
  printf("\n");

  l=0;
  for (i=1;i<sm->sizePsi+1;i++) {
    if (fabs(new_constraint[i])>1E-10) l++; // non-zero
  }
  words = (WORD*)my_malloc(sizeof(WORD)*(l+1)); 
  assert(words!=NULL);
  k=0;
  for (i=1;i<sm->sizePsi+1;i++) {
    if (fabs(new_constraint[i])>1E-10) {
      words[k].wnum = i;
      words[k].weight = new_constraint[i]; 
      k++;
    }
  }
  words[k].wnum = 0;
  words[k].weight = 0.0;
  fvec = create_svector(words, NULL, 1);

  free(words);
  free(new_constraint);

  return(fvec); 

}


double cutting_plane_algorithm(double *w, long m, int MAX_ITER, double C, /*double epsilon,*/ SVECTOR **fycache, EXAMPLE *ex, STRUCTMODEL *sm, STRUCT_LEARN_PARM *sparm) {
  long i,j;
  double *alpha;
  double **G; /* Gram matrix */
  DOC **dXc; /* constraint matrix */
  double *delta; /* rhs of constraints */
  SVECTOR *new_constraint;
  double dual_obj/*, alphasum*/;
  int iter, size_active, no_violation_iter; 
  double value;
  //int r;
  //int *idle; /* for cleaning up */
  double margin;
  //double primal_obj;
  double lower_bound, approx_upper_bound;
  double *proximal_rhs;
  //double *gammaG0=NULL;
  //double min_rho = 0.001;
  //double max_rho;
  //double serious_counter=0;
  //double rho = 1.0;

  //double expected_descent, primal_obj_b=-1, reg_master_obj;
  //int null_step=1;
  //double *w_b;
  //double kappa=0.01;
  //double temp_var;
  //double proximal_term, primal_lower_bound;

  //double v_k; 
  //double obj_difference; 
  // double *cut_error; // cut_error[i] = alpha_{k,i} at current center x_k
  //double sigma_k;
  //double m2 = 0.2;
  //double m3 = 0.9;
  //double gTd; 
  //double last_sigma_k=0; 

  //double initial_primal_obj;
  //int suff_decrease_cond=0;
  //double decrease_proportion = 0.2; // start from 0.2 first 

  //double z_k_norm;
  //double last_z_k_norm=0;


  /*
  w_b = create_nvector(sm->sizePsi);
  clear_nvector(w_b,sm->sizePsi);
  // warm start
  for (i=1;i<sm->sizePsi+1;i++) {
    w_b[i] = w[i];
  }*/

  iter = 0;
  no_violation_iter = 0;
  size_active = 0;
  alpha = NULL;
  G = NULL;
  dXc = NULL;
  delta = NULL;
  //idle = NULL;

  proximal_rhs = NULL;
  //cut_error = NULL; 

  new_constraint = find_cutting_plane(ex, fycache, &margin, m, sm, sparm);
  value = margin - sprod_ns(w, new_constraint);
	
  //primal_obj_b = 0.5*sprod_nn(w_b,w_b,sm->sizePsi)+C*value;
  //primal_obj = 0.5*sprod_nn(w,w,sm->sizePsi)+C*value;
  //primal_lower_bound = 0;
  //expected_descent = -primal_obj_b;
  //initial_primal_obj = primal_obj_b; 

  //max_rho = C; 

  // Non negative weight constraints
  int nNonNeg = sm->sizePsi;
  G = (double**)malloc(sizeof(double*)*nNonNeg);
  for (j=0;j<nNonNeg;j++) {
    G[j] = (double*)malloc(sizeof(double)*nNonNeg);
    for (int k=0;k<nNonNeg;k++) {
	  G[j][k] = 0;
    }

    G[j][j] = 1.0;
  }
  double* alphabeta = NULL;

  while (/*(!suff_decrease_cond)&&(expected_descent<-epsilon)&&*/(iter<MAX_ITER)&&(no_violation_iter<MAX_INNER_ITER_NO_VIOLATION)) {
	LearningTracker::NextInnerIteration();
    iter+=1;
    size_active+=1;

#if (DEBUG_LEVEL>0)
    printf("INNER ITER %d\n", iter); 
#endif

    /* add  constraint */
    dXc = (DOC**)realloc(dXc, sizeof(DOC*)*size_active);
    assert(dXc!=NULL);
    dXc[size_active-1] = (DOC*)malloc(sizeof(DOC));
    dXc[size_active-1]->fvec = new_constraint; 
    dXc[size_active-1]->slackid = 1; // only one common slackid (one-slack)
    dXc[size_active-1]->costfactor = 1.0;

    delta = (double*)realloc(delta, sizeof(double)*size_active);
    assert(delta!=NULL);
    delta[size_active-1] = margin;

	alphabeta = (double*)realloc(alphabeta, sizeof(double)*(size_active+nNonNeg));
	assert(alphabeta!=NULL);
    alphabeta[size_active+nNonNeg-1] = 0.0;

    /*idle = (int*)realloc(idle, sizeof(int)*size_active);
    assert(idle!=NULL); 
    idle[size_active-1] = 0;*/

    /* proximal point */
    proximal_rhs = (double*)realloc(proximal_rhs, sizeof(double)*(size_active+nNonNeg));
    assert(proximal_rhs!=NULL); 
    
	/*cut_error = (double*)realloc(cut_error, sizeof(double)*size_active); 
    assert(cut_error!=NULL); 
    // note g_i = - new_constraint
    cut_error[size_active-1] = C*(sprod_ns(w_b, new_constraint) - sprod_ns(w, new_constraint)); 
    cut_error[size_active-1] += (primal_obj_b - 0.5*sprod_nn(w_b,w_b,sm->sizePsi)); 
    cut_error[size_active-1] -= (primal_obj - 0.5*sprod_nn(w,w,sm->sizePsi)); */

    /*gammaG0 = (double*)realloc(gammaG0, sizeof(double)*size_active);
    assert(gammaG0!=NULL);*/
      
    /* update Gram matrix */
    G = (double**)realloc(G, sizeof(double*)*(size_active+nNonNeg));
    assert(G!=NULL);
    G[size_active+nNonNeg-1] = NULL;
    for (j=0;j<size_active+nNonNeg;j++) {
      G[j] = (double*)realloc(G[j], sizeof(double)*(size_active+nNonNeg));
      assert(G[j]!=NULL);
    }
    for (j=0;j<size_active-1;j++) {
      G[size_active+nNonNeg-1][j+nNonNeg] = sprod_ss(dXc[size_active-1]->fvec, dXc[j]->fvec);
      G[j+nNonNeg][size_active+nNonNeg-1] = G[size_active+nNonNeg-1][j+nNonNeg];
    }
    G[size_active+nNonNeg-1][size_active+nNonNeg-1] = sprod_ss(dXc[size_active-1]->fvec,dXc[size_active-1]->fvec);

	for (j=0;j<nNonNeg;j++) {
	  WORD indicator[2];
	  indicator[0].wnum = j + 1;
	  indicator[0].weight = 1.0;
	  indicator[1].wnum = 0;
	  indicator[1].weight = 0.0;
	  SVECTOR* indicator_vec = create_svector(indicator, NULL, 1.0);
	  G[size_active+nNonNeg-1][j] = sprod_ss(dXc[size_active-1]->fvec, indicator_vec);
	  G[j][size_active+nNonNeg-1] = G[size_active+nNonNeg-1][j];
	  free_svector(indicator_vec);
	}
	
    /* update gammaG0 */
    /*if (null_step==1) {
      gammaG0[size_active-1] = sprod_ns(w_b, dXc[size_active-1]->fvec);
    } else {
      for (i=0;i<size_active;i++) {
	    gammaG0[i] = sprod_ns(w_b, dXc[i]->fvec); 
      }
    }*/

     /* update proximal_rhs */
    for (i=0;i<size_active;i++) {
      proximal_rhs[i+nNonNeg] = -delta[i]; //(1+rho) * (rho * gammaG0[i] - (1 + rho) * delta[i]);
    }
	for (i=0;i<nNonNeg;i++) {
	  proximal_rhs[i] = 0; //w_b[i + 1]*rho * (1+rho);
	}

	/* DEBUG */
	/*
	for (i = 0; i < size_active + nNonNeg; ++i) {
		printf("G[%d]=", i);
		for (j = 0; j < size_active + nNonNeg; ++j) {
			printf("%.4f ", G[i][j]);
		}
		printf("\n");
	}
	printf("\n");
	for (i = 0; i < size_active + nNonNeg; ++i)
		printf("proximal_rhs[%d]=%.4f\n", i, proximal_rhs[i]);
	*/

    /* solve QP to update alpha */
    dual_obj = 0; 
    mosek_qp_optimize(G, proximal_rhs, alphabeta, (long) size_active+nNonNeg, C, &dual_obj, nNonNeg);
	printf("dual_obj=%.4lf\n", dual_obj);

	alpha = alphabeta + nNonNeg;

    clear_nvector(w,sm->sizePsi);
	for (i = 0; i < nNonNeg; i++) {
	  w[i + 1] = alphabeta[i];//alphabeta[i]/(1+rho);  // add betas
	}
    for (j=0;j<size_active;j++) {
      if (alpha[j]>C*ALPHA_THRESHOLD) {
		//add_vector_ns(w,dXc[j]->fvec,alpha[j]/(1+rho));
		  add_vector_ns(w,dXc[j]->fvec,alpha[j]);
      }
    }

    //z_k_norm = sqrt(sprod_nn(w,w,sm->sizePsi)); 

    //add_vector_nn(w, w_b, sm->sizePsi, rho/(1+rho));

    LearningTracker::ReportWeights(w, sm->sizePsi);

    /* detect if step size too small */
    /*
	sigma_k = 0; 
    alphasum = 0; 
    for (j=0;j<size_active;j++) {
      sigma_k += alpha[j]*cut_error[j]; 
      alphasum+=alpha[j]; 
    }
    sigma_k/=C; 
    gTd = -C*(sprod_ns(w,new_constraint) - sprod_ns(w_b,new_constraint));

#if (DEBUG_LEVEL>0)
    for (j=0;j<size_active;j++) {
      printf("alpha[%d]: %.8g, cut_error[%d]: %.8g\n", j, alpha[j], j, cut_error[j]);
    }
    printf("sigma_k: %.8g\n", sigma_k); 
    printf("alphasum: %.8g\n", alphasum);
    printf("g^T d: %.8g\n", gTd); 
    fflush(stdout); 
#endif
	*/

    /* update cleanup information */
    /*
	for (j=0;j<size_active;j++) {
      if (alpha[j]<ALPHA_THRESHOLD*C) {
	idle[j]++;
      } else {
        idle[j]=0;
      }
    }
	*/

	// update lower bound
	double xi = -1e+20;
	for (i = 0; i < size_active; ++i) {
		xi = MAX(xi, delta[i] - sprod_ns(w, dXc[i]->fvec));
	}
	lower_bound = 0.5*sprod_nn(w,w,sm->sizePsi)+C*xi;
	printf("lower_bound=%.4lf\n", lower_bound);
	assert(fabs(lower_bound + dual_obj) < 1e-6);
	LearningTracker::ReportLowerBound(lower_bound);

    // find new constraint
	new_constraint = find_cutting_plane(ex, fycache, &margin, m, sm, sparm);
    value = margin - sprod_ns(w, new_constraint);
	double violation = value - xi;
	if (violation > CUTTING_PLANE_EPS) {
		printf("New constraint is violated by %.4lf\n", violation);
		no_violation_iter = 0;
	} else {
		++no_violation_iter;
		printf("New constraint is underviolated by %.4lf\n", violation);
		printf("%d more such constraints to stop\n", MAX_INNER_ITER_NO_VIOLATION - no_violation_iter);
	}
	
	// update upper bound
	approx_upper_bound = 0.5*sprod_nn(w,w,sm->sizePsi)+C*value;
	printf("approx_upper_bound=%.4lf\n", approx_upper_bound);
	LearningTracker::ReportUpperBound(approx_upper_bound);
         
	/*
    temp_var = sprod_nn(w_b,w_b,sm->sizePsi); 
    proximal_term = 0.0;
    for (i=1;i<sm->sizePsi+1;i++) {
      proximal_term += (w[i]-w_b[i])*(w[i]-w_b[i]);
    }
    
    reg_master_obj = -dual_obj+0.5*rho*temp_var/(1+rho);
    expected_descent = reg_master_obj - primal_obj_b;

    v_k = (reg_master_obj - proximal_term*rho/2) - primal_obj_b; 

    primal_lower_bound = MAX(primal_lower_bound, reg_master_obj - 0.5*rho*(1+rho)*proximal_term);
	LearningTracker::ReportLowerBoundValue(reg_master_obj);

#if (DEBUG_LEVEL>0)
    printf("ITER REG_MASTER_OBJ: %.4f\n", reg_master_obj);
    printf("ITER EXPECTED_DESCENT: %.4f\n", expected_descent);
    printf("ITER PRIMAL_OBJ_B: %.4f\n", primal_obj_b);
    printf("ITER RHO: %.4f\n", rho);
    printf("ITER ||w-w_b||^2: %.4f\n", proximal_term);
    printf("ITER PRIMAL_LOWER_BOUND: %.4f\n", primal_lower_bound);
    printf("ITER V_K: %.4f\n", v_k); 
#endif
    obj_difference = primal_obj - primal_obj_b; 
	
    if (primal_obj<primal_obj_b+kappa*expected_descent) {
      // extra condition to be met
      if ((gTd>m2*v_k)||(rho<min_rho+1E-8)) {
#if (DEBUG_LEVEL>0)
	printf("SERIOUS STEP\n");
#endif
	// update cut_error
	for (i=0;i<size_active;i++) {
	  cut_error[i] -= (primal_obj_b - 0.5*sprod_nn(w_b,w_b,sm->sizePsi)); 
	  cut_error[i] -= C*sprod_ns(w_b, dXc[i]->fvec); 
	  cut_error[i] += (primal_obj - 0.5*sprod_nn(w,w,sm->sizePsi));
	  cut_error[i] += C*sprod_ns(w, dXc[i]->fvec); 
	}
	primal_obj_b = primal_obj;
	for (i=1;i<sm->sizePsi+1;i++) {
	  w_b[i] = w[i];
	}
	null_step = 0;
	serious_counter++;	
      } else {
	// increase step size
#if (DEBUG_LEVEL>0)
	printf("NULL STEP: SS(ii) FAILS.\n");
#endif
	serious_counter--; 
	rho = MAX(rho/10,min_rho);
      }
    } else { // no sufficient decrease
      serious_counter--; 
      if ((cut_error[size_active-1]>m3*last_sigma_k)&&(fabs(obj_difference)>last_z_k_norm+last_sigma_k)) {
#if (DEBUG_LEVEL>0)
	printf("NULL STEP: NS(ii) FAILS.\n");
#endif
	rho = MIN(10*rho,max_rho);
      } 
#if (DEBUG_LEVEL>0)
      else printf("NULL STEP\n");
#endif
    }
    // update last_sigma_k
    last_sigma_k = sigma_k; 
    last_z_k_norm = z_k_norm; 


    // break away from while loop if more than certain proportioal decrease in primal objective
    if (primal_obj_b/initial_primal_obj<1-decrease_proportion) {
      suff_decrease_cond = 1; 
    }

    // clean up
    if (iter % CLEANUP_CHECK == 0) {
      size_active = resize_cleanup(size_active, idle, alpha, delta, gammaG0, proximal_rhs, G, dXc, cut_error);
    }
	*/
	  
  } // end cutting plane while loop 

  printf("Inner loop optimization finished.\n"); fflush(stdout); 
      
  /* free memory */
  for (j=0;j<size_active;j++) {
    free(G[j]);
    free_example(dXc[j],0);	
  }
  free(G);
  free(dXc);
  free(alphabeta);
  free(delta);
  free_svector(new_constraint);
  //free(idle);
  //free(gammaG0);
  free(proximal_rhs);
  //free(cut_error); 

  /* copy and free */
  /*for (i=1;i<sm->sizePsi+1;i++) {
    w[i] = w_b[i];
  }
  free(w_b);*/

  //return(primal_obj_b);
  return lower_bound;
}



int main(int argc, char* argv[]) {

  double *w; /* weight vector */
  int outer_iter;
  long m, i;
  double C, epsilon;
  LEARN_PARM learn_parm;
  KERNEL_PARM kernel_parm;
  char trainfile[1024];
  char modelfile[1024];
  int MAX_ITER;
  /* new struct variables */
  SVECTOR **fycache, *diff, *fy;
  EXAMPLE *ex;
  SAMPLE sample;
  STRUCT_LEARN_PARM sparm;
  STRUCTMODEL sm;
  
  //double decrement;
  double primal_obj;//, last_primal_obj;
  //double cooling_eps; 
  //double stop_crit; 
 
  DebugConfiguration::VerbosityLevel = VerbosityLevel::None;

  /* read input parameters */
  my_read_input_parameters(argc, argv, trainfile, modelfile, &learn_parm, &kernel_parm, &sparm); 

  epsilon = learn_parm.eps;
  C = learn_parm.svm_c;
  MAX_ITER = learn_parm.maxiter;

  /* read in examples */
  sample = read_struct_examples(trainfile,&sparm); 
  ex = sample.examples;
  m = sample.n;
  
  /* initialization */
  init_struct_model(sample,&sm,&sparm,&learn_parm,&kernel_parm); 
  w = sm.w;

  //w = create_nvector(sm.sizePsi);
  //clear_nvector(w, sm.sizePsi);
  //sm.w = w; /* establish link to w, as long as w does not change pointer */

  /* some training information */
  printf("C: %.8g\n", C);
  printf("epsilon: %.8g\n", epsilon);
  printf("sample.n: %ld\n", sample.n); 
  printf("sm.sizePsi: %ld\n", sm.sizePsi); fflush(stdout);

  /* impute latent variable for first iteration */
  init_latent_variables(&sample,&learn_parm,&sm,&sparm);
  
  /* prepare feature vector cache for correct labels with imputed latent variables */
  fycache = (SVECTOR**)malloc(m*sizeof(SVECTOR*));
  for (i=0;i<m;i++) {
    fy = psi(ex[i].x, ex[i].y, ex[i].h, &sm, &sparm);

	/* DEBUG */
	printf("true_psi[%d]=", i);
	for (int j = 0; j < sm.sizePsi; ++j)
		printf("%.4lf ", fy->words[j].weight);
	printf("\n");

    diff = add_list_ss(fy);
    free_svector(fy);
    fy = diff;
    fycache[i] = fy;
  }
    
  /* outer loop: latent variable imputation */
  outer_iter = 1;
  //last_primal_obj = 0;
  //decrement = 0;
  //cooling_eps = 0.5*C*epsilon; 
  //while ((outer_iter<=MIN_OUTER_ITER)||((!stop_crit)&&(outer_iter<MAX_OUTER_ITER))) {
  while (outer_iter<MAX_OUTER_ITER) {
	LearningTracker::NextOuterIteration();
    printf("OUTER ITER %d\n", outer_iter); 
    /* cutting plane algorithm */
    primal_obj = cutting_plane_algorithm(w, m, MAX_ITER, C, /*cooling_eps, */fycache, ex, &sm, &sparm); 
    
    /* compute decrement in objective in this outer iteration */
    /*
	decrement = last_primal_obj - primal_obj;
    last_primal_obj = primal_obj;
    printf("primal objective: %.4f\n", primal_obj);
    printf("decrement: %.4f\n", decrement); fflush(stdout);
    stop_crit = (decrement<C*epsilon)&&(cooling_eps<0.5*C*epsilon+1E-8);
    cooling_eps = -decrement*0.01;
    cooling_eps = MAX(cooling_eps, 0.5*C*epsilon);
    printf("cooling_eps: %.8g\n", cooling_eps); */

	/* print new weights */
	printf("W=");
    for (i = 1; i <= sm.sizePsi; ++i)
		printf("%.3f ", sm.w[i]);
	printf("\n");

	/* Save model */
	char modelfile_tmp[1024];
	sprintf(modelfile_tmp, "%s.%d", modelfile, outer_iter);
	write_struct_model(modelfile_tmp, &sm, &sparm);
	
	/* impute latent variable using updated weight vector */
    for (i=0;i<m;i++) {
      free_latent_var(ex[i].h);
      ex[i].h = infer_latent_variables(ex[i].x, ex[i].y, &sm, &sparm);
    }
    /* re-compute feature vector cache */
    for (i=0;i<m;i++) {
      free_svector(fycache[i]);
      fy = psi(ex[i].x, ex[i].y, ex[i].h, &sm, &sparm);

	  /* DEBUG */
	  printf("true_psi[%d]=", i);
	  for (int j = 0; j < sm.sizePsi; ++j)
		printf("%.4lf ", fy->words[j].weight);
	  printf("\n");

      diff = add_list_ss(fy);
      free_svector(fy);
      fy = diff;
      fycache[i] = fy;
    }

    outer_iter++;  
  } // end outer loop
  

  /* write structural model */
  write_struct_model(modelfile, &sm, &sparm);
  // skip testing for the moment  

  /* free memory */
  free_struct_sample(sample);
  free_struct_model(sm, &sparm);
  for(i=0;i<m;i++) {
    free_svector(fycache[i]);
  }
  free(fycache);
   
  return(0); 
  
}



void my_read_input_parameters(int argc, char *argv[], char *trainfile, char* modelfile,
			      LEARN_PARM *learn_parm, KERNEL_PARM *kernel_parm, STRUCT_LEARN_PARM *struct_parm) {
  
  long i;

  /* set default */
  learn_parm->maxiter=20000;
  learn_parm->svm_maxqpsize=100;
  learn_parm->svm_c=100.0;
  learn_parm->eps=0.001;
  learn_parm->biased_hyperplane=12345; /* store random seed */
  learn_parm->remove_inconsistent=10; 
  kernel_parm->kernel_type=0;
  kernel_parm->rbf_gamma=0.05;
  kernel_parm->coef_lin=1;
  kernel_parm->coef_const=1;
  kernel_parm->poly_degree=3;

  struct_parm->custom_argc=0;

  for(i=1;(i<argc) && ((argv[i])[0] == '-');i++) {
    switch ((argv[i])[1]) {
    case 'c': i++; learn_parm->svm_c=atof(argv[i]); break;
    case 'e': i++; learn_parm->eps=atof(argv[i]); break;
    case 's': i++; learn_parm->svm_maxqpsize=atol(argv[i]); break; 
    case 'g': i++; kernel_parm->rbf_gamma=atof(argv[i]); break;
    case 'd': i++; kernel_parm->poly_degree=atol(argv[i]); break;
    case 'r': i++; learn_parm->biased_hyperplane=atol(argv[i]); break; 
    case 't': i++; kernel_parm->kernel_type=atol(argv[i]); break;
    case 'n': i++; learn_parm->maxiter=atol(argv[i]); break;
    case 'p': i++; learn_parm->remove_inconsistent=atol(argv[i]); break; 
    case '-': strcpy(struct_parm->custom_argv[struct_parm->custom_argc++],argv[i]);i++; strcpy(struct_parm->custom_argv[struct_parm->custom_argc++],argv[i]);break; 
    default: printf("\nUnrecognized option %s!\n\n",argv[i]);
      exit(0);
    }

  }

  if(i>=argc) {
    printf("\nNot enough input parameters!\n\n");
    my_wait_any_key();
    exit(0);
  }
  strcpy (trainfile, argv[i]);

  if((i+1)<argc) {
    strcpy (modelfile, argv[i+1]);
  }
  
  parse_struct_parameters(struct_parm);

}



void my_wait_any_key()
{
  printf("\n(more)\n");
  (void)getc(stdin);
}


/*
int resize_cleanup(int size_active, int *idle, double *alpha, double *delta, double *gammaG0, double *proximal_rhs, double **G, DOC **dXc, double *cut_error) {
  int i,j, new_size_active;
  long k;

  i=0;
  while ((i<size_active)&&(idle[i]<IDLE_ITER)) i++;
  j=i;
  while((j<size_active)&&(idle[j]>=IDLE_ITER)) j++;

  while (j<size_active) {
    // copying
    alpha[i] = alpha[j];
    delta[i] = delta[j];
    gammaG0[i] = gammaG0[j];
    cut_error[i] = cut_error[j]; 
    
    free(G[i]);
    G[i] = G[j]; 
    G[j] = NULL;
    free_example(dXc[i],0);
    dXc[i] = dXc[j];
    dXc[j] = NULL;

    i++;
    j++;
    while((j<size_active)&&(idle[j]>=IDLE_ITER)) j++;
  }
  for (k=i;k<size_active;k++) {
    if (G[k]!=NULL) free(G[k]);
    if (dXc[k]!=NULL) free_example(dXc[k],0);
  }
  new_size_active = i;
  alpha = (double*)realloc(alpha, sizeof(double)*new_size_active);
  delta = (double*)realloc(delta, sizeof(double)*new_size_active);
  gammaG0 = (double*)realloc(gammaG0, sizeof(double)*new_size_active);
  proximal_rhs = (double*)realloc(proximal_rhs, sizeof(double)*new_size_active);
  G = (double**)realloc(G, sizeof(double*)*new_size_active);
  dXc = (DOC**)realloc(dXc, sizeof(DOC*)*new_size_active);
  cut_error = (double*)realloc(cut_error, sizeof(double)*new_size_active); 
  
  // resize G and idle
  i=0;
  while ((i<size_active)&&(idle[i]<IDLE_ITER)) i++;
  j=i;
  while((j<size_active)&&(idle[j]>=IDLE_ITER)) j++;

  while (j<size_active) {
    idle[i] = idle[j];
    for (k=0;k<new_size_active;k++) {
      G[k][i] = G[k][j];
    }
    i++;
    j++;
    while((j<size_active)&&(idle[j]>=IDLE_ITER)) j++;
  }  
  idle = (int*)realloc(idle, sizeof(int)*new_size_active);
  for (k=0;k<new_size_active;k++) {
    G[k] = (double*)realloc(G[k], sizeof(double)*new_size_active);
  }
  return(new_size_active);

}
*/